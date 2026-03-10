-- ============================================================
-- Cleeny - Product Catalog Reset Script
-- ============================================================
-- Deletes ALL products and every record that references them:
--   sales, quotations, stock entries, adjustment entries,
--   inventory, label jobs, images, wholesale prices, surcharges.
--
-- HOW TO USE:
--   1. Run as-is -> ends with ROLLBACK, no real changes.
--      Check pre/post counts in output.
--   2. When ready: comment ROLLBACK, uncomment COMMIT. Re-run.
--
-- WARNING: Irreversible once committed. Take a backup first.
--
-- Does NOT delete: customers, locations, suppliers, payment
--   methods, cash registers, users, or configuration tables.
-- ============================================================


-- ============================================================
-- PRE-CHECK (outside transaction)
-- ============================================================
USE App;
SELECT 'sh_products'                  AS tbl, COUNT(*) AS cnt FROM sh_products;
SELECT 'sh_product_images'            AS tbl, COUNT(*) AS cnt FROM sh_product_images;
SELECT 'sh_product_wholesale_prices'  AS tbl, COUNT(*) AS cnt FROM sh_product_wholesale_prices;
SELECT 'sh_product_partial_surcharges'AS tbl, COUNT(*) AS cnt FROM sh_product_partial_surcharges;
SELECT 'sh_partial_sale_fractions'    AS tbl, COUNT(*) AS cnt FROM sh_partial_sale_fractions;
SELECT 'sh_inventory'                 AS tbl, COUNT(*) AS cnt FROM sh_inventory;
SELECT 'sh_inventory_movements'       AS tbl, COUNT(*) AS cnt FROM sh_inventory_movements;
SELECT 'sh_stock_entry_items'         AS tbl, COUNT(*) AS cnt FROM sh_stock_entry_items;
SELECT 'sh_stock_entries'             AS tbl, COUNT(*) AS cnt FROM sh_stock_entries;
SELECT 'sh_adjustment_entry_items'    AS tbl, COUNT(*) AS cnt FROM sh_adjustment_entry_items;
SELECT 'sh_adjustment_entries'        AS tbl, COUNT(*) AS cnt FROM sh_adjustment_entries;
SELECT 'sh_bulk_label_jobs'           AS tbl, COUNT(*) AS cnt FROM sh_bulk_label_jobs;
-- SELECT 'sh_quotation_details'         AS tbl, COUNT(*) AS cnt FROM sh_quotation_details;
-- SELECT 'sh_quotations'                AS tbl, COUNT(*) AS cnt FROM sh_quotations;
SELECT 'sh_sale_details'              AS tbl, COUNT(*) AS cnt FROM sh_sale_details;
SELECT 'sh_sale_payments'             AS tbl, COUNT(*) AS cnt FROM sh_sale_payments;
SELECT 'sh_sales'                     AS tbl, COUNT(*) AS cnt FROM sh_sales;
SELECT 'mx_invoices'                  AS tbl, COUNT(*) AS cnt FROM mx_invoices;
SELECT 'mx_invoice_files'             AS tbl, COUNT(*) AS cnt FROM mx_invoice_files;


-- ============================================================
-- DELETION (wrapped in transaction)
-- ============================================================
BEGIN;

-- 1. CFDI invoice files (cascade child of mx_invoices)
DELETE FROM mx_invoice_files;

-- 2. CFDI invoices (FK Restrict -> sh_sales, must go before sales)
DELETE FROM mx_invoices;

-- 3. Sales (cascades to sh_sale_details and sh_sale_payments)
DELETE FROM sh_sales;

-- 4. Quotations (cascades to sh_quotation_details)
-- DELETE FROM sh_quotations;

-- 5. Bulk label jobs (FK Restrict -> sh_products)
DELETE FROM sh_bulk_label_jobs;

-- 6. Entry items (FK Restrict -> sh_inventory_movements AND sh_products)
--    Must go before inventory_movements to avoid FK violation
DELETE FROM sh_adjustment_entry_items;
DELETE FROM sh_stock_entry_items;

-- 7. Inventory movements (FK Restrict -> sh_products, sh_stock_entries, sh_adjustment_entries)
--    Must go before stock/adjustment entries to avoid FK violation
DELETE FROM sh_inventory_movements;

-- 8. Inventory stock levels (FK Restrict -> sh_products)
DELETE FROM sh_inventory;

-- 9. Adjustment entries (now empty, orphan cleanup)
DELETE FROM sh_adjustment_entries;

-- 10. Stock entries (now empty, orphan cleanup)
DELETE FROM sh_stock_entries;

-- 12. Product child tables (FK Cascade -> sh_products, explicit for clarity)
DELETE FROM sh_product_images;
DELETE FROM sh_product_wholesale_prices;
DELETE FROM sh_product_partial_surcharges;

-- 13. Products (main table)
DELETE FROM sh_products;

-- 14. Partial sale fractions (product catalog seed data)
DELETE FROM sh_partial_sale_fractions;

-- Optional: reset auto-increment counters (uncomment if needed)
ALTER TABLE sh_products                   AUTO_INCREMENT = 1;
ALTER TABLE sh_product_images             AUTO_INCREMENT = 1;
ALTER TABLE sh_product_wholesale_prices   AUTO_INCREMENT = 1;
ALTER TABLE sh_product_partial_surcharges AUTO_INCREMENT = 1;
ALTER TABLE sh_partial_sale_fractions     AUTO_INCREMENT = 1;
ALTER TABLE sh_inventory                  AUTO_INCREMENT = 1;
ALTER TABLE sh_inventory_movements        AUTO_INCREMENT = 1;
ALTER TABLE sh_stock_entry_items          AUTO_INCREMENT = 1;
ALTER TABLE sh_stock_entries              AUTO_INCREMENT = 1;
ALTER TABLE sh_adjustment_entry_items     AUTO_INCREMENT = 1;
ALTER TABLE sh_adjustment_entries         AUTO_INCREMENT = 1;
ALTER TABLE sh_bulk_label_jobs            AUTO_INCREMENT = 1;
-- ALTER TABLE sh_quotation_details          AUTO_INCREMENT = 1;
-- ALTER TABLE sh_quotations                 AUTO_INCREMENT = 1;
ALTER TABLE sh_sale_details               AUTO_INCREMENT = 1;
ALTER TABLE sh_sale_payments              AUTO_INCREMENT = 1;
ALTER TABLE sh_sales                      AUTO_INCREMENT = 1;
ALTER TABLE mx_invoices                   AUTO_INCREMENT = 1;
ALTER TABLE mx_invoice_files              AUTO_INCREMENT = 1;

-- ROLLBACK;   -- <- no changes saved (safe default)
COMMIT;  -- <- uncomment to apply permanently


-- ============================================================
-- POST-CHECK (outside transaction)
-- ============================================================
SELECT 'sh_products'                  AS tbl, COUNT(*) AS cnt FROM sh_products;
SELECT 'sh_product_images'            AS tbl, COUNT(*) AS cnt FROM sh_product_images;
SELECT 'sh_product_wholesale_prices'  AS tbl, COUNT(*) AS cnt FROM sh_product_wholesale_prices;
SELECT 'sh_product_partial_surcharges'AS tbl, COUNT(*) AS cnt FROM sh_product_partial_surcharges;
SELECT 'sh_partial_sale_fractions'    AS tbl, COUNT(*) AS cnt FROM sh_partial_sale_fractions;
SELECT 'sh_inventory'                 AS tbl, COUNT(*) AS cnt FROM sh_inventory;
SELECT 'sh_inventory_movements'       AS tbl, COUNT(*) AS cnt FROM sh_inventory_movements;
SELECT 'sh_stock_entry_items'         AS tbl, COUNT(*) AS cnt FROM sh_stock_entry_items;
SELECT 'sh_stock_entries'             AS tbl, COUNT(*) AS cnt FROM sh_stock_entries;
SELECT 'sh_adjustment_entry_items'    AS tbl, COUNT(*) AS cnt FROM sh_adjustment_entry_items;
SELECT 'sh_adjustment_entries'        AS tbl, COUNT(*) AS cnt FROM sh_adjustment_entries;
SELECT 'sh_bulk_label_jobs'           AS tbl, COUNT(*) AS cnt FROM sh_bulk_label_jobs;
-- SELECT 'sh_quotation_details'         AS tbl, COUNT(*) AS cnt FROM sh_quotation_details;
-- SELECT 'sh_quotations'                AS tbl, COUNT(*) AS cnt FROM sh_quotations;
SELECT 'sh_sale_details'              AS tbl, COUNT(*) AS cnt FROM sh_sale_details;
SELECT 'sh_sale_payments'             AS tbl, COUNT(*) AS cnt FROM sh_sale_payments;
SELECT 'sh_sales'                     AS tbl, COUNT(*) AS cnt FROM sh_sales;
SELECT 'mx_invoices'                  AS tbl, COUNT(*) AS cnt FROM mx_invoices;
SELECT 'mx_invoice_files'             AS tbl, COUNT(*) AS cnt FROM mx_invoice_files;
