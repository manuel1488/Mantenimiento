using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWholesalePriceIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Single-column sanity CHECK constraints ──────────────────────────────
            // A CHECK cannot reference another table, so the cross-table invariant
            // (wholesale FixedPrice < retail Price) is enforced by triggers below.
            migrationBuilder.Sql(
                "ALTER TABLE `sh_product_wholesale_prices` " +
                "ADD CONSTRAINT `ck_wholesale_fixedprice_positive` " +
                "CHECK (`FixedPrice` IS NULL OR `FixedPrice` > 0);");

            migrationBuilder.Sql(
                "ALTER TABLE `sh_product_wholesale_prices` " +
                "ADD CONSTRAINT `ck_wholesale_discount_range` " +
                "CHECK (`DiscountPercentage` >= 0 AND `DiscountPercentage` <= 100);");

            migrationBuilder.Sql(
                "ALTER TABLE `sh_product_wholesale_prices` " +
                "ADD CONSTRAINT `ck_wholesale_minquantity_positive` " +
                "CHECK (`MinQuantity` > 0);");

            // ── Cross-table invariant: wholesale FixedPrice must be below retail ─────
            // A wholesale price >= retail would yield a negative discount (a surcharge)
            // that corrupts document totals. Legitimate surcharges use a separate field
            // (SurchargePercentage), so this is always a data error.
            // Only enforced on active, non-deleted tiers so a bad row can still be
            // deactivated to fix it.
            migrationBuilder.Sql(@"
CREATE TRIGGER `trg_wholesale_price_insert_check`
BEFORE INSERT ON `sh_product_wholesale_prices`
FOR EACH ROW
BEGIN
    DECLARE v_retail DECIMAL(10,6);
    IF NEW.FixedPrice IS NOT NULL AND NEW.IsActive = 1 AND NEW.IsDeleted = 0 THEN
        SELECT Price INTO v_retail FROM sh_products WHERE Id = NEW.ProductId;
        IF v_retail IS NOT NULL AND NEW.FixedPrice >= v_retail THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Wholesale fixed price must be below the product retail price';
        END IF;
    END IF;
END;");

            migrationBuilder.Sql(@"
CREATE TRIGGER `trg_wholesale_price_update_check`
BEFORE UPDATE ON `sh_product_wholesale_prices`
FOR EACH ROW
BEGIN
    DECLARE v_retail DECIMAL(10,6);
    IF NEW.FixedPrice IS NOT NULL AND NEW.IsActive = 1 AND NEW.IsDeleted = 0 THEN
        SELECT Price INTO v_retail FROM sh_products WHERE Id = NEW.ProductId;
        IF v_retail IS NOT NULL AND NEW.FixedPrice >= v_retail THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Wholesale fixed price must be below the product retail price';
        END IF;
    END IF;
END;");

            // Reverse direction: lowering a product's retail price must not reach or fall
            // below an active wholesale fixed price. Gated on an actual price change so it
            // never blocks unrelated edits of products that still hold legacy bad data.
            migrationBuilder.Sql(@"
CREATE TRIGGER `trg_product_price_update_check`
BEFORE UPDATE ON `sh_products`
FOR EACH ROW
BEGIN
    DECLARE v_cnt INT;
    IF NEW.Price <> OLD.Price THEN
        SELECT COUNT(*) INTO v_cnt FROM sh_product_wholesale_prices
        WHERE ProductId = NEW.Id AND IsActive = 1 AND IsDeleted = 0
          AND FixedPrice IS NOT NULL AND FixedPrice >= NEW.Price;
        IF v_cnt > 0 THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Retail price cannot be at or below an active wholesale fixed price';
        END IF;
    END IF;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS `trg_product_price_update_check`;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS `trg_wholesale_price_update_check`;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS `trg_wholesale_price_insert_check`;");

            migrationBuilder.Sql(
                "ALTER TABLE `sh_product_wholesale_prices` DROP CHECK `ck_wholesale_minquantity_positive`;");
            migrationBuilder.Sql(
                "ALTER TABLE `sh_product_wholesale_prices` DROP CHECK `ck_wholesale_discount_range`;");
            migrationBuilder.Sql(
                "ALTER TABLE `sh_product_wholesale_prices` DROP CHECK `ck_wholesale_fixedprice_positive`;");
        }
    }
}
