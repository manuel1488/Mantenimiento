"""
Genera un archivo CSV para carga masiva de inventario inicial.
Los codigos de producto corresponden a los generados por generate_test_products.py
usando el prefijo "P" con padding de 4 digitos (P0001, P0002, ...).

Uso: python tools/generate_test_inventory.py

Formato CSV (columnas en espanol, matching InventoryColumnMappingService.es.resx):
  - Codigo de Producto
  - Cantidad
  - Stock Minimo
  - Stock Maximo
"""

import csv
import os


def generate_test_inventory():
    # Inventario inicial de prueba
    # Formato: (codigo_producto, cantidad, stock_minimo, stock_maximo)
    #
    # Cantidades razonables para una tienda de productos de limpieza:
    # - Productos populares: mas stock
    # - Productos especializados: menos stock
    # - A granel: cantidades en litros/kilos

    inventory = [
        # ---- CLORO Y BLANQUEADORES (P0001-P0005) ----
        ("P0001", 50, 10, 100),    # Cloro Concentrado 1L
        ("P0002", 30, 6, 60),      # Cloro Concentrado 4L
        ("P0003", 40, 8, 80),      # Cloro Gel 750ml
        ("P0004", 36, 8, 72),      # Blanqueador Lavanda 1L
        ("P0005", 36, 8, 72),      # Blanqueador Pino 1L

        # ---- DESINFECTANTES Y ANTIBACTERIALES (P0006-P0013) ----
        ("P0006", 40, 10, 80),     # Desinfectante Multiusos 750ml
        ("P0007", 35, 8, 70),      # Desinfectante Multiusos 1L
        ("P0008", 20, 5, 40),      # Desinfectante Multiusos 4L
        ("P0009", 30, 6, 60),      # Desinfectante Spray 400ml
        ("P0010", 60, 15, 120),    # Antibacterial Manos 250ml
        ("P0011", 40, 10, 80),     # Antibacterial Manos 500ml
        ("P0012", 25, 6, 50),      # Antibacterial Manos 1L
        ("P0013", 36, 8, 72),      # Toallitas Desinfectantes x80

        # ---- JABONES (P0014-P0019) ----
        ("P0014", 48, 12, 96),     # Jabon Liquido Manos 250ml
        ("P0015", 36, 8, 72),      # Jabon Liquido Manos 500ml
        ("P0016", 24, 6, 48),      # Jabon Liquido Manos 1L
        ("P0017", 60, 15, 120),    # Jabon Barra Lavaplatos 350g
        ("P0018", 36, 8, 72),      # Jabon Lavaplatos Liquido 750ml
        ("P0019", 24, 6, 48),      # Jabon Lavaplatos Liquido 1.5L

        # ---- DETERGENTES Y SUAVIZANTES (P0020-P0025) ----
        ("P0020", 40, 10, 80),     # Detergente Polvo 1kg
        ("P0021", 15, 4, 30),      # Detergente Polvo 5kg
        ("P0022", 30, 8, 60),      # Detergente Liquido 1L
        ("P0023", 18, 4, 36),      # Detergente Liquido 3L
        ("P0024", 30, 8, 60),      # Suavizante 850ml
        ("P0025", 18, 4, 36),      # Suavizante 3L

        # ---- LIMPIADORES ESPECIALIZADOS (P0026-P0035) ----
        ("P0026", 40, 10, 80),     # Limpiador Pino 1L
        ("P0027", 20, 5, 40),      # Limpiador Pino 4L
        ("P0028", 30, 8, 60),      # Limpiador Vidrios 750ml
        ("P0029", 30, 8, 60),      # Desengrasante Cocina 750ml
        ("P0030", 30, 8, 60),      # Limpiador Banos 750ml
        ("P0031", 24, 6, 48),      # Limpiador Pisos Ceramicos 1L
        ("P0032", 20, 5, 40),      # Limpiador Acero Inoxidable 500ml
        ("P0033", 24, 6, 48),      # Destapa Canos 500ml
        ("P0034", 48, 12, 96),     # Acido Muriatico 1L
        ("P0035", 24, 6, 48),      # Sarricida 500ml

        # ---- AROMATIZANTES (P0036-P0040) ----
        ("P0036", 24, 6, 48),      # Aromatizante Primavera 400ml
        ("P0037", 24, 6, 48),      # Aromatizante Lavanda 400ml
        ("P0038", 24, 6, 48),      # Aromatizante Citrico 400ml
        ("P0039", 60, 15, 120),    # Pastilla WC Pino
        ("P0040", 60, 15, 120),    # Pastilla WC Lavanda

        # ---- PAPEL Y DESECHABLES (P0041-P0048) ----
        ("P0041", 50, 12, 100),    # Toallas Rollo x100
        ("P0042", 24, 6, 48),      # Toallas Rollo x250
        ("P0043", 36, 10, 72),     # Papel Higienico x4
        ("P0044", 18, 4, 36),      # Papel Higienico x12
        ("P0045", 30, 8, 60),      # Servilletas x500
        ("P0046", 50, 12, 100),    # Bolsa Basura 60x90 x25
        ("P0047", 40, 10, 80),     # Bolsa Basura 90x120 x10
        ("P0048", 24, 6, 48),      # Bolsa Basura 90x120 x25

        # ---- UTENSILIOS DE LIMPIEZA (P0049-P0064) ----
        ("P0049", 48, 12, 96),     # Esponja Pack x3
        ("P0050", 30, 8, 60),      # Esponja Pack x6
        ("P0051", 60, 15, 120),    # Fibra Verde x3
        ("P0052", 30, 8, 60),      # Fibra Verde x10
        ("P0053", 40, 10, 80),     # Estropajo Acero x3
        ("P0054", 12, 3, 24),      # Escoba Plastico
        ("P0055", 10, 3, 20),      # Escoba Mijo Natural
        ("P0056", 15, 4, 30),      # Recogedor
        ("P0057", 10, 3, 20),      # Trapeador Microfibra
        ("P0058", 10, 3, 20),      # Trapeador Algodon
        ("P0059", 15, 4, 30),      # Repuesto Trapeador
        ("P0060", 20, 5, 40),      # Cubeta 12L
        ("P0061", 15, 4, 30),      # Cubeta 20L
        ("P0062", 40, 10, 80),     # Atomizador 500ml
        ("P0063", 12, 3, 24),      # Cepillo WC con Base
        ("P0064", 8, 2, 16),       # Jalador Pisos 40cm

        # ---- GUANTES Y PROTECCION (P0065-P0069) ----
        ("P0065", 15, 4, 30),      # Guantes Latex x100 Ch
        ("P0066", 20, 5, 40),      # Guantes Latex x100 Med
        ("P0067", 15, 4, 30),      # Guantes Latex x100 Gde
        ("P0068", 40, 10, 80),     # Guantes Hule Amarillo Par
        ("P0069", 24, 6, 48),      # Cubrebocas x50

        # ---- KITS Y JUEGOS (P0070-P0072) ----
        ("P0070", 12, 3, 24),      # Kit Limpieza Basico
        ("P0071", 10, 3, 20),      # Kit Limpieza Bano
        ("P0072", 8, 2, 16),       # Juego Escoba y Recogedor

        # ---- A GRANEL LIQUIDOS en L (P0073-P0083) ----
        ("P0073", 200, 50, 500),   # Cloro Industrial a Granel
        ("P0074", 200, 50, 500),   # Cloro Regular a Granel
        ("P0075", 100, 30, 300),   # Desengrasante Industrial a Granel
        ("P0076", 150, 40, 400),   # Jabon Liquido Manos a Granel
        ("P0077", 150, 40, 400),   # Jabon Lavaplatos a Granel
        ("P0078", 100, 30, 300),   # Suavizante a Granel
        ("P0079", 200, 50, 500),   # Pinol a Granel
        ("P0080", 200, 50, 500),   # Limpiador Pisos a Granel
        ("P0081", 150, 40, 400),   # Acido Muriatico a Granel
        ("P0082", 100, 30, 300),   # Aromatizante a Granel
        ("P0083", 150, 40, 400),   # Detergente Liquido a Granel

        # ---- A GRANEL SOLIDOS en KG (P0084-P0087) ----
        ("P0084", 100, 25, 250),   # Detergente Polvo a Granel
        ("P0085", 50, 15, 150),    # Bicarbonato a Granel
        ("P0086", 30, 10, 80),     # Sosa Caustica a Granel
        ("P0087", 80, 20, 200),    # Sal Industrial a Granel
    ]

    # Encabezados en espanol (matching InventoryColumnMappingService.es.resx)
    headers = [
        "Código de Producto",
        "Cantidad",
        "Stock Mínimo",
        "Stock Máximo",
    ]

    # Guardar CSV
    output_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), "docs")
    os.makedirs(output_dir, exist_ok=True)
    output_path = os.path.join(output_dir, "inventario_inicial_carga_masiva.csv")

    with open(output_path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(headers)
        for item in inventory:
            writer.writerow(item)

    # Estadisticas
    total = len(inventory)
    total_qty = sum(i[1] for i in inventory)
    granel_l = sum(1 for i in inventory if int(i[0][1:]) >= 73 and int(i[0][1:]) <= 83)
    granel_kg = sum(1 for i in inventory if int(i[0][1:]) >= 84)
    unitarios = total - granel_l - granel_kg

    print(f"Archivo generado: {output_path}")
    print(f"Total productos:  {total}")
    print(f"  - Unitarios:         {unitarios}")
    print(f"  - A granel (L):      {granel_l}")
    print(f"  - A granel (KG):     {granel_kg}")
    print(f"Total unidades/cantidad: {total_qty:,.0f}")


if __name__ == "__main__":
    generate_test_inventory()
