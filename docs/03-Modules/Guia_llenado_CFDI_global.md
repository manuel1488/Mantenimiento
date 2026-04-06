# Guía de llenado del

# CFDI global

# Versión 4.0 del CFDI


## Contenido

_Introducción_ ............................................................................................................................................... 3

_I. Guía de llenado del CFDI global versión 4.0_ ...................................................................... 6

_Glosario_ ........................................................................................................................................................ 23

_Apéndice 1 Notas Generales_ ......................................................................................................... 24

_Apéndice 2 Caso de Uso CFDI de egreso aplicable a un CFDI global_ ................ 25

_Apéndice 3 Instrucciones específicas de llenado en el CFDI global aplicable_

_a Hidrocarburos y Petrolíferos_ ...................................................................................................... 37

_Control de cambios a la Guía de llenado del CFDI global Versión 4.0 del_

_CFDI_ .............................................................................................................................................................. 44


```
Introducción
```
Expedir Comprobantes Fiscales Digitales por Internet (CFDI) por los actos o

actividades realizadas, por los ingresos que perciban o por las retenciones de

contribuciones que efectúen los contribuyentes, es una obligación de los

contribuyentes personas físicas o morales de conformidad con el artículo 29,

párrafos primero y segundo, fracción IV y penúltimo párrafo del Código Fiscal de

la Federación (CFF) y 39 del Reglamento del CFF, en relación con la regla 2.7.1. 21 .,

y el Capítulo 2.7 De los Comprobantes Fiscales Digitales por Internet o Factura

Electrónica de la Resolución Miscelánea Fiscal (RMF) vigente.

El artículo 29 - A del CFF en su primer párrafo, fracción IV establece el requisito de

incluir en el comprobante fiscal la clave en el RFC del receptor del mismo, dicha

disposición aclara que cuando no se cuente con dicha clave en el RFC se señalará

la clave genérica que establezca el Servicio de Administración Tributaria (SAT)

mediante reglas de carácter general, dicha clave está publicada en la regla

2.7.1.2 3. de la RMF vigente.

No obstante lo señalado y considerando que conforme a lo establecido en el

artículo 29, penúltimo párrafo del citado Código, el SAT mediante reglas de

carácter general podrá establecer facilidades administrativas para que los

contribuyentes emitan sus comprobantes fiscales por medios propios, a través de

proveedores de servicios o con medios electrónicos que en dichas reglas

determine, dicho Órgano Administrativo Desconcentrado ha establecido en la

regla 2.7.1.2 1. de la RMF vigente una facilidad para el caso de las operaciones en

donde no se cuenta con la clave en el RFC del receptor, consistente en poder

emitir a sus clientes que no solicitan un CFDI en éstas operaciones que se

conocen como “celebradas con el público en general”, un comprobante de

operaciones con el público en general que no puede ser utilizado para deducir o

acreditar, estando esto condicionado a tener que elaborar también un CFDI

global en donde consten:

```
 Los importes correspondientes a cada una de las operaciones realizadas
con el público en general del periodo.
```
```
 El número de folio o de operación de cada uno de los comprobantes de
operaciones con el público en general que se emitieron en el periodo.
```
```
 Emitir este CFDI global incluyendo como clave en el RFC de su receptor la
clave genérica en el RFC para CFDI global a que se refiere la regla 2.7.1.2 3
de la RMF vigente.
```

El CFDI global deberá expedirse a más tardar a las 24 horas siguientes al cierre de

las operaciones que lo integran.

El monto del Impuesto al Valor Agregado (IVA) y del Impuesto Especial sobre

Producción y Servicios (IEPS) deberá estar desglosado en forma expresa y por

separado en los CFDI globales.

Cuando los adquirentes de los bienes o receptores de los servicios no soliciten

comprobantes de operaciones realizadas con el público en general, los

contribuyentes no estarán obligados a expedirlos cuando el importe sea inferior

a $100.00 (cien pesos 00/100 M.N.), no obstante deberán incluir dentro del CFDI

global en donde consten las operaciones celebradas con el público en general del

peridodo que corresponda.

El CFDI global en donde consten las operaciones celebradas con el público en

general, deberá remitirse al SAT o al proveedor de certificación de CFDI, según

sea el caso, dentro de las 24 horas siguientes al cierre de las operaciones

realizadas de manera diaria, semanal,mensual o bimestral.

Los documentos técnicos, especifican la estructura, forma y sintaxis que deben

contener los CFDI que expidan los contribuyentes (personas físicas y morales), lo

cual permite que la información se introduzca de manera organizada en el

comprobante.

En la sección I de este documento se describe cómo se debe realizar el llenado

de los datos a registrar en el CFDI global en su versión 4.0.

En el caso de alguna duda o situación particular sobre el llenado del comprobante

global que no se encuentre resuelta en esta guía, el contribuyente debe remitirse

a los siguientes documentos, mismos que se encuentran publicados en el portal

del SAT.

```
 Documentación técnica del Anexo 20.
```
```
 Preguntas y respuestas de los comprobantes fiscales digitales por
Internet.
```
```
 Casos de uso de los comprobantes fiscales digitales por Internet.
```
La presente guía de llenado es un documento cuyo objeto es explicar a los

contribuyentes la forma correcta de llenar y expedir un CFDI, observando las

definiciones del estándar tecnológico del Anexo 20 y las disposiciones jurídicas

vigentes aplicables, para ello se hace uso de ejemplos que faciliten las

explicaciones, por ello es importante aclarar que los datos usados para los


ejemplos son ficticios y únicamente para efectos didácticos a fin de explicar de

manera fácil cómo se llena un CFDI.

Por lo anteriormente señalado, el lector debe tener claro que las explicaciones

realizadas en esta Guía de llenado no sustituyen a las disposiciones fiscales

legales o reglamentarias vigentes, por lo que en temas distintos a la forma

correcta de llenar y expedir un CFDI, como pueden ser los relativos a la

determinación de las contribuciones, los sujetos, el objeto, las tasas, las tarifas, las

mecánicas de cálculo, los requisitos de las deducciones, entre otros, los

contribuyentes deberán observar las disposiciones fiscales vigentes aplicables.


**_I. Guía de llenado del CFDI global versión 4._**

```
Cuando se emita un CFDI, se debe hacer con las especificaciones señaladas en
cada uno de los campos expresados en lenguaje no informático que se incluyen
en esta sección.
```
```
En el presente documento se hace referencia a la descripción de la información
que debe contener el CFDI.
```
```
Cuando en las siguientes descripciones se establezca el uso de un valor, éste se
señala entre comillas, pero en el CFDI debe registrarse sin incluir las comillas,
respetar mayúsculas, minúsculas, números, espacios y signos de puntuación.
```
```
Nombre del
nodo o
atributo
```
```
Descripción
```
```
Nodo:
Comprobante
```
```
Formato estándar del Comprobante Fiscal Digital por Internet.
```
```
Version Debe tener el valor “ 4.0”.
```
```
Este dato lo integra el sistema que utiliza el contribuyente para
la emisión del comprobante fiscal.
```
```
Serie Es el número de serie que utiliza el contribuyente para control
interno de su información. Este campo acepta de uno hasta 25
caracteres alfanuméricos.
```
```
Folio Es el folio de control interno que asigna el contribuyente al
comprobante, puede conformarse desde uno hasta 40
caracteres alfanuméricos.
```
```
Fecha Es la fecha y hora de expedición del comprobante fiscal. Se
expresa en la forma AAAA-MM-DDThh:mm:ss y debe
corresponder con la hora local donde se expide el comprobante.
```
```
Este dato lo integra el sistema que utiliza el contribuyente para
la emisión del comprobante fiscal.
```
```
Ejemplo:
Fecha= 2022 - 01 - 27T11:49:
```
```
Sello Es el sello digital del comprobante fiscal generado con el
certificado de sello digital del contribuyente emisor del
```

```
comprobante; éste funge como la firma del emisor del
comprobante y lo integra el sistema que utiliza el contribuyente
para la emisión del comprobante.
```
FormaPago Se debe registrar la clave de forma de pago con la que se liquidó
el comprobante de operaciones con el público en general de
mayor monto de entre los contenidos en el CFDI global, en caso
de haber dos o más comprobantes con el mismo monto pero
distintas formas de pago, el contribuyente podrá registrar a su
consideración una de las formas de pago con las que se recibió
el mismo.

```
Las diferentes claves de forma de pago se encuentran incluidas
en el catálogo c_FormaPago.
```
```
Ejemplo:
FormaPago= 02
```
```
c_FormaPago Descripción
```
```
01 Efectivo
```
```
02 Cheque nominativo
```
```
03 Transferencia
electrónica de fondos
```
NoCertificado Es el número que identifica al certificado de sello digital del
emisor, el cual lo incluye en el comprobante fiscal el sistema que
utiliza el contribuyente para la emisión.

Certificado Es el contenido del certificado del sello digital del emisor y lo
integra el sistema que utiliza el contribuyente para la emisión
del comprobante fiscal.

CondicionesDe
Pago

```
Este campo no debe existir.
```

SubTotal Es la suma de los importes de los conceptos antes de
descuentos e impuestos. No se permiten valores negativos.

```
Este campo debe tener hasta la cantidad de decimales que
soporte la moneda, ver ejemplo del campo Moneda.
```
```
El importe registrado en este campo debe ser igual al
redondeo de la suma de los importes de los conceptos
registrados.
```
Descuento
Se puede registrar el importe total de los descuentos
aplicables antes de impuestos. No se permiten valores
negativos. Se debe registrar cuando existan conceptos con
descuento.

```
 Este campo debe tener hasta la cantidad de decimales
que soporte la moneda, ver ejemplo del campo Moneda.
 El valor registrado en este campo debe ser menor o
igual que el campo Subtotal.
 Cuando en el campo TipoDeComprobante sea “I”
(Ingreso) y algún concepto incluya un Descuento, este
campo debe existir y debe ser igual al redondeo de la
suma de los campos Descuento registrados en los
conceptos; en otro caso se debe omitir este campo.
```
Moneda Se debe registrar la clave de la moneda utilizada para expresar
los montos, cuando se usa moneda nacional se registra “MXN”,
conforme con la especificación ISO 4217.

```
Las distintas claves de moneda se encuentran incluidas en el
catálogo c_Moneda.
```
```
Ejemplo:
Moneda= MXN
```
```
c_Moneda Descripció
n
```
```
Decimales Porcentaje
variación
USD Dólar
Americano
```
```
2 35%
```
```
MXN Peso
Mexicano
```
```
2 35%
```
(^)
TipoCambio Se puede registrar el tipo de cambio FIX conforme a la moneda
registrada en el comprobante.
Este campo es requerido cuando la clave de moneda es distinta
de “MXN” (Peso Mexicano) y a la clave “XXX” (Los códigos


```
asignados para las transacciones en que intervenga ninguna
moneda).
```
```
Si el valor está fuera del porcentaje aplicable a la moneda
tomado del catálogo c_Moneda, el emisor debe obtener del
proveedor de certificación de CFDI que vaya a timbrar el CFDI
de manera no automática, una clave de confirmación para
ratificar que el valor es correcto e integrar dicha clave en el
campo Confirmacion.
```
```
El límite superior se obtiene al multiplicar el valor publicado del
tipo de cambio FIX por la suma de uno más el porcentaje
aplicable a la moneda tomado del catálogo c_Moneda.
```
```
El límite inferior se obtiene al multiplicar el valor publicado del
tipo de cambio FIX por la suma de uno menos el porcentaje
aplicable a la moneda tomado del catálogo c_Moneda. Si este
límite fuera negativo se toma cero.
```
```
Nota importante:
Esta validación estará vigente únicamente a partir de que el
SAT publique en su Portal los procedimientos para generar
la clave de confirmación y parametrizar los rangos máximos
aplicables.
```
Total Es la suma del subtotal, menos los descuentos aplicables, más
las contribuciones recibidas (impuestos). No se permiten
valores negativos.

```
Este campo debe tener hasta la cantidad de decimales que
soporte la moneda, ver ejemplo del campo Moneda.
Cuando el valor equivalente en “MXN” (Peso Mexicano) de este
campo exceda el límite establecido, debe existir el campo
Confirmacion.
```
```
Nota importante:
Esta validación estará vigente únicamente a partir de que el
SAT publique en su Portal los procedimientos para generar
la clave de confirmación y para parametrizar los montos
máximos aplicables.
```
```
Si se hacen retenciones no se puede aplicar la facilidad de la
regla 2.7.1.2 1. de la RMF vigente y debe emitirse un CFDI por cada
operación.
```

TipoDeCompro
bante

```
Se debe registrar la clave “I” (Ingreso) de conformidad con el
catálogo c_TipoDeComprobante.
```
Exportacion Se debe registrar la clave “ 01” (No aplica).

**MetodoPago** Se debe registrar siempre la clave “PUE” (Pago en una sola
exhibición), de conformidad con el catálogo c_MetodoPago.

```
Ejemplo:
MetodoPago= PUE
```
```
c_MetodoPago Descripción
PUE Pago en una sola exhibición
```
(^)
LugarExpedicio
n
Se debe registrar el código postal del lugar de expedición del
comprobante (domicilio de la matriz o de la sucursal), debe
corresponder a una clave de código postal vigente incluida en
el catálogo c_CodigoPostal.
En el caso de que se emita un comprobante fiscal en una
sucursal, en dicho comprobante se debe registrar el código
postal de ésta, independientemente de que los sistemas de
facturación de la empresa se encuentren en un domicilio
distinto al de la sucursal.
Al ingresar el código postal en este campo se cumple con el
requisito de señalar el domicilio y lugar de expedición del
comprobante a que se refieren las fracciones I y III del primer
párrafo del Artículo 29-A del CFF, en los términos de la regla
2.7.1. 29 ., fracción I de la Resolución Miscelánea Fiscal vigente.
Los distintos códigos postales se encuentran incluidos en el
catálogo c_CodigoPostal.
En caso de que dentro del catálogo c_CodigoPostal, no se
encuentre contenida información del código postal, se debe
registrar la clave del código postal más cercana del lugar de
expedición del comprobante fiscal.
**Ejemplo:**
LugarExpedicion= **01000
c_CodigoPostal
01000**


Confirmacion Se debe registrar la clave de confirmación única e irrepetible
que entrega el proveedor de certificación de CFDI o el SAT a los
emisores (usuarios) para expedir el comprobante con importes
o tipo de cambio fuera del rango establecido o en ambos casos.

```
Ejemplo:
Confirmacion= ECVH
```
```
Se pueden registrar valores alfanuméricos a cinco
posiciones.
```
```
Nota importante:
El uso de esta clave estará vigente únicamente a partir de
que el SAT publique en su Portal los procedimientos para
generar la clave de confirmación y parametrizar los montos
y rangos máximos aplicables.
```
**Nodo:Informac
ionGlobal**

```
En este nodo se debe expresar la información relacionada con
el comprobante global de operaciones con el público en
general.
```
Periodicidad Campo requerido para registrar el período al que corresponde
la información del comprobante global.

```
Cuando el valor de este campo sea “05” el campo RegimenFiscal
debe ser “621”.
```
```
Ejemplo:
```
```
Periodicidad= 02
c_Periodicidad Descripción
02 Semanal
```
Meses Se debe registrar la clave del mes o los meses al que
corresponde la información de las operaciones celebradas con
el público en general, las distintas claves vigentes se encuentran
incluidas en el catálogo c_Meses.

```
 Cuando el valor del campo Periodicidad sea “05”
(Bimestral), este campo debe contener alguno de los
valores “13” (Enero-Febrero), “14” (Marzo-Abril), “15” (Mayo-
```

```
Junio), “16” (Julio-Agosto), “17” (Septiembre-Octubre) o
“18” (Noviembre-Diciembre).
```
```
 Si el campo Periodicidad contiene un valor diferente de
“05”, (Bimestral) este campo debe contener alguno de los
valores “01” (Enero), “02”(Febrero), “03” (Marzo),
“04”(Abril), “05”(Mayo), “06”(Junio), “07” (Julio),
“08”(Agosto), “09”(Septiembre), “10” (Octubre), “11”
(Noviembre) o “12” (Diciembre).
```
```
Ejemplo:
```
```
Meses= 05
```
```
c_Meses Descripción
05 Mayo
```
Año Se debe registrar el año al que corresponde la información del
comprobante global.

```
El valor registrado debe ser igual al año en curso o al año
inmediato anterior considerando el registrado en la Fecha de
emisión del comprobante.
```
```
Ejemplo:
```
```
Año= 2022
```
**Nodo:
CfdiRelacionad
os**

```
En este nodo se puede expresar la información de los
comprobantes fiscales relacionados.
```
TipoRelacion Se debe registrar la clave de la relación que existe entre éste
comprobante (factura global) que se está generando y el o los
CFDI previos que tienen alguna relación entre sí.

```
La clave de Tipo de relación se encuentra incluida en el catálogo
c_TipoRelacion publicado en el Portal del SAT.
```
```
Cuando el tipo de relación tenga la clave “04” sustitución, el
comprobante que se está generando (factura global) como
sustituto es de tipo “I” (Ingreso).
Ejemplo:
TipoRelacion= 04
```

```
c_TipoRelacion Descripción
04 Sustitución de los CFDI previos
```
**Nodo:
CfdiRelacionad
o**

```
En este nodo se debe expresar la información de los
comprobantes fiscales relacionados.
```
UUID Se debe registrar el folio fiscal (UUID) de un comprobante fiscal
relacionado con el presente comprobante.

```
Ejemplo:
UUID= 5FB2822E-396D- 4725 - 8521 - CDC4BDD20CCF
```
**Nodo: Emisor** En este nodo se debe expresar la información del contribuyente
que emite el comprobante fiscal.

Rfc Se debe registrar la clave en el Registro Federal de
Contribuyentes del emisor del comprobante.

```
En el caso de que el emisor sea una persona física, este campo
debe contener una longitud de 13 posiciones, si se trata de
personas morales debe contener una longitud de 12 posiciones.
```
```
Ejemplo:
```
```
En el caso de una persona física se debe registrar:
Rfc= CABL840215RF
```
```
En el caso de una persona moral se debe registrar:
Rfc= PAL7202161U
```
Nombre Se debe registrar el nombre, denominación o razón social
inscrito en el RFC del emisor del comprobante.

```
El Nombre debe corresponder a la clave de RFC registrado en el
campo Rfc de este Nodo.
```
```
Ejemplo:
```
```
En el caso de una persona física se debe registrar:
Nombre= MARTON ALEEJANDRO SANZI FIERROR
```
```
En el caso de una persona moral se debe registrar:
Nombre= LA PALMA AEI
```

RegimenFiscal Se debe especificar la clave vigente del régimen fiscal del
contribuyente emisor bajo el cual se está emitiendo el
comprobante.

```
Las claves de los diversos regímenes se encuentran incluidas
en el catálogo c_RegimenFiscal publicado en el Portal del SAT.
```
```
Ejemplo: En el caso de que el emisor sea una persona moral
inscrita en el Régimen General de Ley de Personas Morales,
debe registrar lo siguiente:
```
```
RegimenFiscal= 601
```
```
Aplica para tipo persona
```
```
c_RegimenFiscal Descripción Física Moral
```
```
601 General de Ley
Personas Morales
```
```
No Sí
```
```
603 Personas Morales
con Fines no
Lucrativos
```
```
No Sí
```
```
605 Sueldos y Salarios
e Ingresos
Asimilados a
Salarios
```
```
SÍ No
```
(^)
FacAtrAdquire
nte
Este campo no debe existir.
**Nodo:
Receptor**
En este nodo se debe expresar la información del contribuyente
receptor del comprobante.
Rfc
Se debe registrar el valor “XAXX010101000”.
**Ejemplo:**
Rfc= **XAXX**
Nombre En este campo se debe de registrar el valor “PUBLICO EN
GENERAL”.
DomicilioFiscal
Receptor
En este campo se debe registrar el mismo código postal
señalado en el campo LugarExpedicion.
**Ejemplo:**


```
DomicilioFiscalReceptor= 01000
```
ResidenciaFisc
al

```
Este campo no debe existir.
```
NumRegIdTrib Este campo no debe existir.

RegimenFiscal
Receptor

```
En este campo se debe registrar la clave “616” (Sin
obligaciones fiscales).
```
UsoCFDI Se debe registrar la clave “S01” (Sin efectos fiscales).

(^)
**Nodo:
Conceptos**
En este nodo se deben expresar los conceptos descritos en el
comprobante.
**Nodo:
Concepto**
En este nodo se debe expresar la información detallada de cada
uno de los comprobantes de operaciones con el público en
general.
ClaveProdServ En este campo se debe registrar la clave “01010101” del catálogo
c_ClaveProdServ publicado en el Portal del SAT.
**Ejemplo:**
ClaveProdServ= **01010101
c_ClaveProdServ
Descripció
n
Incluir IVA
trasladado
Incluir IEPS
trasladado
01010101** No existe en el catálogo Opcional Opcional
NoIdentificacio
n
En este campo se debe registrar el número de folio o de
operación de los comprobantes de operación con el público en
general. Puede conformarse desde uno hasta 100 caracteres
alfanuméricos.
**Ejemplo:**
NoIdentificacion= **150**
Cantidad
Se debe registrar el valor “1”
**Ejemplo:**
Cantidad= **1**


ClaveUnidad Se debe registrar la clave “ACT” del catálogo c_ClaveUnidad
publicado en el Portal del SAT.

```
Ejemplo:
ClaveUnidad= ACT
```
```
c_ClaveUnidad Nombre
```
```
ACT Actividad
```
Unidad Este campo no debe existir.

Descripcion Se debe registrar el valor “Venta”.

```
Ejemplo:
Descripcion= Venta
```
ValorUnitario En este campo se debe registrar el subtotal del comprobante
de operaciones con el público en general, el cual puede
contener de cero hasta seis decimales.

```
Si el tipo de comprobante es de “I” (Ingreso), este valor debe
ser mayor a cero.
```
```
Ejemplo:
ValorUnitario= 1230.
```
Importe Debe ser equivalente al resultado de multiplicar la Cantidad por
el ValorUnitario expresado en el concepto, el cual será calculado
por el sistema que genera el comprobante y considerará los
redondeos que tenga registrado este campo. No se permiten
valores negativos.

```
Este campo puede contener de cero hasta seis decimales.
```
```
Ejemplo:
Importe= 1230.
```
```
Cantidad Valor unitario Importe
```
```
1 1230.00 1230.
```
Descuento Se debe registrar el importe de los descuentos aplicables a cada
comprobante de operaciones con el público en general, debe


```
tener hasta la cantidad de decimales que tenga registrado en
el campo importe del concepto y debe ser menor o igual al
campo Importe. No se permiten valores negativos.
```
```
Este campo puede contener de cero hasta seis decimales.
```
```
Ejemplo:
Descuento= 864.
```
```
Cantidad Valor unitario Importe Descuento
```
```
3.141649 1230.00 3864.22827 864.
```
(^)
ObjetoImp Se debe registrar la clave correspondiente para indicar si la
operación comercial es objeto o no de impuesto.
 Las claves vigentes se encuentran incluidas en el
catálogo c_ObjetoImp.
 Si el valor registrado en este campo es “02” (Sí objeto de
impuesto), se deben desglosar los Impuestos a nivel de
Concepto.
 Si el valor registrado en este campo es “01” (No objeto de
impuesto) o “03” (Sí objeto del impuesto y no obligado al
desglose) no se desglosan impuestos a nivel Concepto.
**Ejemplo:**
ObjetoImp= **02**
(^)
**c_ObjetoImp Descripción**
^02 Sí^ objeto de impuesto^
**Nodo:
Impuestos**
En este nodo se pueden expresar los impuestos aplicables a
cada comprobante de operaciones con el público en general.
Si se registra información en este nodo, debe existir la sección:
traslados.
**Nodo:Traslado
s**
En este nodo se pueden expresar los impuestos trasladados
aplicables a cada comprobante de operaciones con el público
en general.
**Nodo:Traslado** En este nodo se debe expresar la información detallada de un
traslado de impuestos correspondiente a cada comprobante
de operaciones con el público en general.


```
En el caso de que un concepto contenga impuesto trasladado
por Tasa o Cuota, se debe expresar en diferentes apartados.
```
```
Nota: Debe considerarse que estos comprobantes no tienen
un receptor en realidad que puediera usarlos para
acreditamiento, por lo que los desgloses de impuestos
trasladados tienen el fin de dar control y fácil identificación de
estos datos a su emisor.
```
Base Se debe registrar el valor para el cálculo del impuesto que se
traslada, puede contener de cero hasta seis decimales.

```
El valor de este campo debe ser mayor que cero.
```
Impuesto Se debe registrar la clave del tipo de impuesto trasladado
aplicable a cada comprobante de operaciones con el público
en general, las cuales se encuentran incluidas en el catálogo
c_Impuesto publicado en el Portal del SAT.

```
Ejemplo:
Impuesto= 002
```
```
c_Impuesto Descripción
```
```
002 IVA
```
```
003 IEPS
```
(^)
TipoFactor Se debe registrar el tipo de factor que se aplica a la base del
impuesto, el cual se encuentra incluido en el catálogo
c_TipoFactor publicado en el Portal del SAT.
**Ejemplo:**
TipoFactor= **Tasa
c_TipoFactor
Tasa**
Cuota
Exento
(^)
TasaOCuota Se puede registrar el valor de la tasa o cuota del impuesto que
se traslada para cada comprobante de operaciones con el
público en general. Es requerido cuando el campo TipoFactor
corresponda a Tasa o Cuota.


```
 Si el valor registrado es fijo debe corresponder al tipo de
impuesto y al tipo de factor conforme al catálogo
c_TasaOCuota.
```
```
 Si el valor registrado es variable, debe corresponder al
rango entre el valor mínimo y el valor máximo señalado
en el catálogo.
```
```
Ejemplo:
TasaOCuota= 0.
```
```
Rango o
Fijo
```
**c_TasaOCuota** (^) **Impuest
Valor mínimo Valor máximo o**^ **Factor**^
Fijo No 0.000000 IVA Tasa
Fijo No **0.160000** IVA Tasa
(^)
Importe Se puede registrar el importe del impuesto trasladado que
aplica a cada concepto. No se permiten valores negativos. Este
campo es requerido cuando en el campo TipoFactor se haya
registrado como Tasa o Cuota.
El valor de este campo será calculado por el sistema que genera
el comprobante y considerará los redondeos que tenga
registrado este campo, para mayor referencia puede consultar
la documentación técnica publicada en el Portal del SAT.
Este campo puede contener de cero hasta seis decimales.
(^)
**Nodo:
Retenciones**
Este nodo no debe existir.
**Nodo:
ACuentaTercer
os**
Este nodo no debe existir.
**Nodo:
InformacionAd
uanera**
Este nodo no debe existir
**Nodo:
CuentaPredial**
Este nodo no debe existir.
**Nodo:
Complemento
Concepto**
Este nodo no debe existir.


**Nodo: Parte** Este nodo no debe existir.

**Nodo:
Impuestos**

```
En este nodo se debe expresar el resumen de los impuestos
aplicables.
```
(^)
TotalImpuestos
Retenidos
Este campo no debe existir.
TotalImpuestos
Trasladados
Es el total de los impuestos trasladados que se desprenden de
los conceptos contenidos en el comprobante fiscal, el cual debe
ser igual a la suma de los importes registrados en la sección
Traslados, no se permiten valores negativos y es requerido
cuando en los conceptos se registren impuestos trasladados.
Este campo debe tener hasta la cantidad de decimales que
soporte la moneda.
**Nodo:
Retenciones**
Este nodo no debe existir.
**Nodo:Traslado
s**
En este nodo se pueden expresar los impuestos trasladados
aplicables y es requerido cuando en los conceptos se registre
un impuesto trasladado.
En el caso de que solo existan conceptos en el CFDI con un
TipoFactor Exento, en este nodo solo deben existir los campos
Base, Impuesto y TipoFactor.
**Nodo: Traslado** En este nodo se debe expresar la información detallada de un
traslado de impuesto específico.
Debe haber solo un registro con la misma combinación de
impuesto, factor y tasa por cada traslado.


**Base** Se debe registrar el monto de la base del impuesto trasladado,
agrupado por Impuesto, TipoFactor y TasaOCuota, el cual debe
tener hasta la cantidad de decimales que soporte la moneda.

```
No se permiten valores negativos y debe ser igual al redondeo
de la suma de los importes de los campos Base trasladados
registrados en los conceptos, donde el impuesto del concepto
sea igual al campo Impuesto de este apartado y la TasaOCuota
del concepto sea igual al campo TasaOCuota de este apartado.
```
```
Ejemplo: Por cada tipo de impuesto se debe registrar el importe
que corresponda, en el caso de servicios contables el importe de
la base que le corresponde es de $15,000.00.
```
```
En caso de que solo existan conceptos con TipoFactor Exento,
la suma de este atributo debe ser igual al redondeo de la suma
de los importes de los campos Base registrados en los
conceptos.
```
```
Base Impuesto TipoF
actor
```
```
TasaO
Cuota
```
```
Importe
```
```
15000
.00
```
```
XXXX XXXX XXXX XXXX
```
```
Base= 15000.00
```
Impuesto Se debe registrar la clave del tipo de impuesto trasladado,
mismas que se encuentran incluidas en el catálogo c_Impuesto
publicado en el Portal del SAT.

**Ejemplo:**
Impuesto= **002**
TipoFactor Se debe registrar el tipo factor que se aplica a la base del
impuesto, mismos que se encuentran incluidos en el catálogo
c_TipoFactor publicado en el Portal del SAT.

```
Ejemplo:
TipoFactor= Tasa
```
TasaOCuota Se puede registrar el valor de la tasa o cuota del impuesto que
se traslada por cada comprobante de operaciones con el
público en general registrado en el comprobante, mismo que
se encuentra incluido en el catálogo c_TasaOCuota publicado
en el Portal del SAT.


```
El valor de la tasa o cuota que se registre debe corresponder a
un registro donde la columna impuesto corresponda con el
campo Impuesto y la columna factor corresponda con el campo
TipoFactor.
```
```
Ejemplo:
TasaOCuota= 0.160000
```
Importe Se puede registrar el monto del impuesto trasladado, agrupado
por Impuesto, TipoFactor y TasaOCuota, el cual debe tener
hasta la cantidad de decimales que soporte la moneda, no se
permiten valores negativos y debe ser igual al redondeo de la
suma de los importes de los impuestos trasladados registrados
en los conceptos donde el impuesto del concepto sea igual al
campo impuesto de este apartado y la TasaOCuota del
concepto sea igual al campo TasaOCuota de este apartado.

```
Ejemplo:
Importe= 2400.00
```
**Nodo:
Complemento**

```
En este nodo el Proveedor de Certificación de CFDI debe incluir
el complemento Timbre Fiscal Digital de manera obligatoria y
en su caso, el SAT debe incluir el complemento CFDI Registro
Fiscal cuando el CFDI global se emita a través de la herramienta
electrónica “Factura Fácil”.
```
**Nodo:
Addenda**

```
En este nodo se puede expresar las extensiones al presente
formato que sean de utilidad al contribuyente. Para las reglas
de uso del mismo, referirse a la documentación técnica.
```

```
Glosario
```
**Nodo, elemento, apartado o sección:** Conjunto de datos.

**Atributo o campo:** Es un dato.

**FIX:** Es el tipo de cambio determinado por el Banco de México para solventar

obligaciones determinadas.


```
Apéndice 1 Notas Generales
```
**Nota 1:** El documento incluye ejemplos de carácter didáctico y hace uso de
información ficticia para ello.


```
Apéndice 2 Caso de Uso CFDI de egreso aplicable a un CFDI global
```
### Disposiciones Generales

Todos los contribuyentes por los actos o actividades que realicen, por los ingresos

que perciban, por el pago de sueldos y salarios o por las retenciones de impuestos

que efectúen, deben emitir comprobantes fiscales mediante documentos

digitales a través de la página de Internet del Servicio de Administración

Tributaria.

En este ejemplo se describe cómo se puede aplicar un CFDI de egreso por un

descuento, devolución, bonificación o cuando se solicite un CFDI nominativo, de

una operación que fue documentada en un CFDI global.

Cabe señalar que observamos tres formas de poder realizar una disminución de

una operación contenida en un CFDI global para generar un CFDI de ingresos de

manera individual y nominativa.

```
I. Ejemplo de CFDI Global con CFDI de egreso relacionado por una
devolución de un concepto.
a) Generación del comprobante de operaciones con el público en general.
b) Emisión de CFDI Global.
c) Emisión CFDI de egresos por la devolución de un concepto contenido en
el comprobante de operaciones con el público en general.
```
```
II. Ejemplo de cancelación de CFDI Global por una solicitud de generación
de CFDI de ingresos por una operación documentada en dicha factura.
a) Cancelación de CFDI Global.
b) Generación del CFDI Global sin considerar el comprobante de
operaciones con el público en general de la operación que se factura de
manera nominativa.
c) Generación del CFDI nominativo por la operación contenida en el
comprobante de operaciones con el público en general.
```
```
III. Ejemplo de CFDI Global con CFDI de egreso relacionado por la emisión de
un CFDI nominativo.
a) Generación del comprobante de operaciones con el público en general.
b) Emisión de CFDI Global.
c) Emisión CFDI de egresos por la operación que se disminuirá para emitir
un CFDI nominativo.
d) Emisión de CFDI nominativo.
```
**Fundamento legal:** Artículos 29 y 29-A del Código Fiscal de la Federación; Regla

2.7.1.2 1. de la Resolución Miscelánea Fiscal vigente.


### PLANTEAMIENTO

```
I.Ejemplo de CFDI Global con CFDI de egreso relacionado por una devolución
de un concepto.
```
```
El contribuyente La Linterna, S.A de C.V., realiza operaciones con público en
general, y expide CFDI haciendo uso de la regla 2.7.1.2 1. El día 11 de junio emitió los
siguientes comprobantes de operaciones con el público en general:
```
```
a) Generación del comprobante de operaciones con el público en general
```
Folio: Folio: Folio:
Fecha: Fecha: Fecha:
Posición: Posición: Posición:
Terminal: Terminal: Terminal:
Responsable: Juan Mirelles R. Responsable: Rosa Melendez Responsable: Juan Mirelles R.

Cantidad ModeloColor/TallaImporte IVA Cantidad Modelo Color/TallaImporte IVA Cantidad Modelo Color/TallaImporte IVA
1 123B 32 $ 130.00 $ 20.80 1 568N 32 $ 130.00 $ 20.80 1 365B 30 $ 179.99 $ 28.80
1 025N 34 $ 225.00 $ 36.00 1 365B 30 $ 179.99 $ 28.80
Subtotal $ 130.00 Subtotal $ 355.00 Subtotal $ 359.98
IVA $ 20.80 IVA $ 56.80 IVA $ 57.60
TOTAL $ 150.80 TOTAL $ 411.80 TOTAL $ 417.58

```
Tarjeta de débito $ 150.80 Tarjeta de Crédito $ 411.80 Tarjeta de Crédito $ 417.58
Cta 1745 Cta 1582 Cta 1582
```
```
Ticket: 17-2358-4568 Ticket: 17-2358-4624 Ticket: 17-2358-4569
```
```
Tels. (06) 89357925,89357926
********************************************
```
```
11/06/2022
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
```
```
22145
```
```
2
2
```
```
********************************************
```
```
********************************************
```
```
********************************************
```
```
********************************************
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
Tels. (06) 89357925,89357926
********************************************
22147
```
```
Tels. (06) 89357925,89357926
********************************************
22146
11/06/2022
5
5
```
```
11/06/2022
2
2
```
```
********************************************
```
```
********************************************
```

El contribuyente generó un CFDI global para documentar las ventas realizadas el día

11 de junio.

```
b) Emisión de CFDI Global
```
```
Con posterioridad a la generación del CFDI global, el cliente Markuz Kuri, realizó
una devolución a la empresa La Linterna S.A. de C.V. de un concepto contenido
en el comprobante de operaciones con el público en general identificado con el
número 17- 2358 - 4569 por un importe de $208.79 con IVA.
```
```
Nombre del Emisor: LA LINTERNA
RFC Emisor:Clave de Regimen Fiscal: LIN820625PU5601, General de Ley Personas Morales
Forma Pago:Método pago: Moneda: MXN
Tipo de comprobante: I Ingreso
```
Lugar de expedición:Fecha y Hora de expedición: (^33146) 2022-06-11 T10:32:08
Nombre del Receptor: PUBLICO EN GENERAL
RFC Receptor:Código Postal del Receptor: XAXX010101000 33146 Tipo Relación: CFDI relacionado:
Régimen Fiscal del Receptor:Uso del CFDI: 616, Sin obligaciones fiscalesS01 Sin efectos fiscales.
Periodicidad: Diario
Meses:Año: 06 Junio 2022
Clav Prod. Serv. No Identificación Cantidad Clave UnidadUnidadDescripción Valor Unit Importe Descuento **Impuesto Objeto** Base Impuesto factorTipo Tipo tasa Importe
01010101 17-2358-4568 1 ACT Venta $ 130.00 $ 130.00 0 02 $ 130.00 002 Tasa 0.160000$ 20.80
01010101 17-2358-4569 1 ACT Venta $ 359.98 $ 359.98 0 02 $ 359.98 002 Tasa 0.160000$ 57.60
01010101 17-2358-4624 1 ACT Venta $ 355.00 $ 355.00 0 02 $ 355.00 002 Tasa 0.160000$ 56.80
SubtotalDescuento $844.98$0.00
Total $135.20$980.18
Sello digital del Emisor:
Sello digital del SAT:
AAAca0c2-3ea4-4506-abe3-f05771737e69
Fecha y Hora del Certificado:
Total de impuestos trasladados:
**Comprobante Fiscal Digital por Internet**
04 Tarjeta de créditoPUE Pago en una sola exhibición
**Folio interno: LNL023**
2022-06-11 T10:32:08
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNMqPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChCTwf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
Cadena original del complemento de certificación digital del SAT:
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-06-11T10:32:08|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||


El contribuyente La Linterna S.A. de C.V., generó un CFDI de egreso por el importe

del concepto en devolución que le está realizando su cliente, el cual se

documentó en el comprobante simplificado con el número 17- 2358 - 4569, y lo

relacionó al CFDI global que integró la operación.

```
Folio:
Fecha:
Posición:
Terminal:
Responsable: Juan Mirelles R.
Cantidad Modelo Color/Talla Importe IVA
1 365B 30 $ 179.99$ 28.80
1 365B 30 $ 179.99$ 28.80
Subtotal $ 359.98
IVA $ 57.60
TOTAL $ 417.58
Tarjeta de Crédito $ 417.58
Cta 1582
```
```
Ticket: 17-2358-4569
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
Tels. (06) 89357925,89357926
********************************************
22147
11/06/2022
2
2
********************************************
```
```
********************************************
```

```
c) Emisión CFDI de egresos por la devolución de un concepto contenido en el
comprobante de operaciones con el público en general.
```
**Nombre del Emisor:** LA LINTERNA
**RFC Emisor:** LIN820625PU5
**Clave de Regimen Fiscal:** 601, General de Ley Personas Morales
**Forma Pago:
Método pago: Moneda** : MXN
**Tipo de comprobante:** E Egreso
**Lugar de expedición:** 33146
**Fecha y Hora de expedición:** 2022-06-20 T13:15:11

**Nombre del Receptor:** PUBLICO EN GENERAL
**RFC Receptor:** XAXX010101000 **Tipo Relación:** 01 Nota de crédito de los documentos relacionados
**Código Postal del Receptor:** 33146 **CFDI relacionado:** AAAca0c2-3ea4-4506-abe3-f05771737e69
**Régimen Fiscal del Receptor:** 616, Sin obligaciones fiscales
**Uso del CFDI:** G02 Devoluciones, descuentos o bonificaciones.

```
Clav Prod. Serv. No IdentificaciónCantidad UnidadClave Unidad DescripciónValor UnitImporte Impuesto Objeto Descuento Base Impuesto factorTipo Tipo tasa Importe
```
```
84111506 17-2358-4569 1 ACT Devolución de mercancias $ 179.99 $ 179.99 02 0 $ 179.99 002 Tasa0.160000$ 28.80
```
**Subtotal** $179.99
**Descuento** $0.00
$28.80
**Total** $208.79

**Sello digital del Emisor:**

**Sello digital del SAT:**

BBBca0c2-3ea4-4899-abe3-f05771737e69

**Fecha y Hora del Certificado:**

**Total de impuestos trasladados:**

**Comprobante Fiscal Digital por Internet**

```
03 Transferencia electrónica de fondos
PUE Pago en una sola exhibición
```
```
Folio interno: LNL036
```
```
2022-06-20 T13:15:11
```
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=

dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=

**Cadena original del complemento de certificación digital del SAT:**

||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-06-20T13:15:11|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||


```
II. Ejemplo de cancelación de factura Global por una solicitud de generación
de un CFDI de ingresos por una operación documentada en dicha factura.
```
En esta opción el contribuyente puede cancelar el CFDI global, para volverlo a

generar sin considerar la operación documentada en el comprobante de

operaciones con el público en general con el número 17- 2358 - 4569, por el cual le

estan solicitando la factura individual.

```
a) Cancelación de CFDI Global
```
```
Se cancela la factura en el Portal del SAT o a través del Proveedor de
Certificación del CFDI, en este ejemplo se cancela la factura con UUID
AAAca0c2-3ea4- 4506 - abe3-f05771737e69.
```
Posteriormente, el contribuyente La Linterna S.A. de C.V., genera la factura global
sin considerar la operación documentada en el comprobante de operaciones con
el público en general con el número 17- 2358 - 4569, ya que por está, emitirá un
CFDI nominativo.

```
b) Generación del CFDI Global sin considerar el comprobante de operaciones con
el público en general de la operación que se factura de manera nominativa
```

Por último, el contribuyente La Linterna S.A. de C.V., genera la factura nominativa
por la operación documentada en el comprobante de operaciones con el público
en general con el número 17- 2358 - 4569.

```
Nombre del Emisor: LA LINTERNA
RFC Emisor: LIN820625PU5
Clave de Regimen Fiscal: 601,General de Ley Personas Morales
Forma Pago:
Método pago: Moneda : MXN
Tipo de comprobante: I Ingreso
Lugar de expedición: 33146
Fecha y Hora de expedición: 2022-06-11 T10:32:08
Nombre del Receptor: PUBLICO EN GENERAL
RFC Receptor: XAXX010101000 Tipo Relación: 04 Sustitución de los CFDI previos
Código Postal del Receptor: 33146 CFDI relacionado: AAAca0c2-3ea4-4506-abe3-f05771737e69
Régimen Fiscal del Receptor: 616, Sin obligaciones fiscales
Uso del CFDI: S01 Sin efectos fiscales.
Periodicidad: Diario
Meses: 06 Junio
Año: 2022
```
```
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad DescripciónValor Unit Importe DescuentoImpuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe
01010101 17-2358-4568 1 ACT Venta $ 130.00$ 130.00 0 02 $ 130.00 002 Tasa 0.160000 $ 20.80
01010101 17-2358-4624 1 ACT Venta $ 355.00 $ 355.00 0 02 $ 355.00 002 Tasa 0.160000 $ 56.80
Subtotal $485.00
Descuento $0.00
$77.60
Total $562.60
Sello digital del Emisor:
```
```
Sello digital del SAT:
```
```
ZZZca0c2-3ea4-1569-abe3-f05771737e56
Fecha y Hora del Certificado:
```
```
Comprobante Fiscal Digital por Internet Folio interno: LNL100
```
```
Total de impuestos trasladados:
```
```
04 Tarjeta de crédito
PUE Pago en una sola exhibición
```
```
2022-06-11 T10:32:08
```
```
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
```
```
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
Cadena original del complemento de certificación digital del SAT:
```
```
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-06-11T10:32:08|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
```

```
c) Generación del CFDI nominativo por la operación contenida en el
comprobante de operaciones con el público en general
```
**Nombre del Emisor:** LA LINTERNA
**RFC Emisor:** LIN820625PU5
**Clave de Regimen Fiscal:** 601,General de Ley Personas Morales
**Forma Pago:
Método pago: Moneda** : MXN
**Tipo de comprobante:** I Ingreso
**Luga de expedición:** 33146
**Fecha y Hora de expedición:** 2022-10-11 T10:32:08

**Nombre del Receptor:** MARKUZ KURI
**RFC Receptor:** MKIZ910508JUM **Tipo Relación:
Código Postal del Receptor:** 33147 **CFDI relacionado:
Régimen Fiscal del Receptor:** 612, Personas Físicas con Actividades Empresariales y Profesionales
**Uso del CFDI:** G03 Gastos en general

```
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad DescripciónValor Unit Importe Descuento Impuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe
```
```
53101604 17-2358-4569 2 H87 Pieza Blusa de mujer $ 179.99$ 359.98 0 02 $ 359.98 002 Tasa 0.160000 $ 57.60
```
**Subtotal** $359.98
**Descuento** $0.00
$57.60
**Total** $417.58

**Sello digital del Emisor:**

**Sello digital del SAT:**

FFFca0c2-3ea4-9856-abe3-f05771737e40

**Fecha y Hora del Certificado:** 2022-10-11 T10:32:08

mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=

dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=

**Cadena original del complemento de certificación digital del SAT:**

||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-10-11T10:32:08|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||

**Comprobante Fiscal Digital por Internet Folio interno: LNL092**

**Total de impuestos trasladados:**

```
01 Efectivo
PUE Pago en una sola exhibición
```

```
III. Ejemplo de CFDI Global con CFDI de egreso relacionado por la emisión de
un CFDI nominativo.
```
```
Para efectos del presente apartado, cuando se requiera disminuir una operación
contenida en un CFDI global, derivado de que el cliente solicite un CFDI
nominativo, el contribuyente podrá emitir un CFDI de egreso para disminuir
dicha operación.
```
```
El contribuyente La Linterna, S.A. de C.V., realiza operaciones con público en
general, y expide CFDI haciendo uso de la regla 2.7.1.21 de la Resolución
Miscelánea Fiscal vigente. El día 11 de junio emitió los siguientes comprobantes
de operaciones con el público en general:
```
```
a) Generación del comprobante de operaciones con el público en general
```
Folio: Folio: Folio:
Fecha: Fecha: Fecha:
Posición: Posición: Posición:
Terminal: Terminal: Terminal:
Responsable: Juan Mirelles R. Responsable: Rosa Melendez Responsable: Juan Mirelles R.

Cantidad ModeloColor/Talla Importe IVA Cantidad Modelo Color/TallaImporte IVA Cantidad Modelo Color/TallaImporte IVA
1 123B 32 $ 130.00 $ 20.80 1 568N 32 $ 130.00 $ 20.80 1 365B 30 $ 179.99 $ 28.80
1 025N 34 $ 225.00 $ 36.00 1 365B 30 $ 179.99 $ 28.80
Subtotal $ 130.00 Subtotal $ 355.00 Subtotal $ 359.98
IVA $ 20.80 IVA $ 56.80 IVA $ 57.60
TOTAL $ 150.80 TOTAL $ 411.80 TOTAL $ 417.58

```
Tarjeta de débito $ 150.80 Tarjeta de Crédito $ 411.80 Tarjeta de Crédito $ 417.58
Cta 1745 Cta 1582 Cta 1582
```
```
Ticket: 17-2358-4568 Ticket: 17-2358-4624 Ticket: 17-2358-4569
```
```
Tels. (06) 89357925,89357926
********************************************
```
```
11/06/2022
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
```
```
22145
```
```
2
2
```
```
********************************************
```
```
********************************************
```
```
********************************************
```
```
********************************************
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
Tels. (06) 89357925,89357926
********************************************
22147
```
```
Tels. (06) 89357925,89357926
********************************************
22146
11/06/2022
5
5
```
```
11/06/2022
2
2
```
```
********************************************
```
```
********************************************
```

El contribuyente generó un CFDI global para documentar las ventas realizadas el día

11 de junio.

```
b) Emisión de CFDI Global
```
```
Con posterioridad a la generación del CFDI global, el cliente Markuz Kuri, solicita
a la empresa La Linterna S.A. de C.V. se le emita el CFDI nominativo de una
operación con el público en general identificada con el número 17- 2358 - 4569 por
un importe de $417.58 con IVA.
```
```
Nombre del Emisor:RFC Emisor: LA LINTERNALIN820625PU5
Clave de Regimen Fiscal:Forma Pago: 601, General de Ley Personas Morales
Método pago:Tipo de comprobante: I Ingreso Moneda: MXN
```
Lugar de expedición:Fecha y Hora de expedición: (^33146) 2022-06-11 T10:32:08
Nombre del Receptor:RFC Receptor: PUBLICO EN GENERALXAXX010101000 Tipo Relación:
Código Postal del Receptor: 33146 CFDI relacionado:
Régimen Fiscal del Receptor:Uso del CFDI: 616, Sin obligaciones fiscalesS01 Sin efectos fiscales.
Periodicidad: Diario
Meses:Año: 06 Junio 2022
Clav Prod. Serv. No Identificación Cantidad Clave UnidadUnidadDescripciónValor Unit Importe Descuento **Impuesto Objeto** Base Impuesto factorTipo Tipo tasa Importe
01010101 17-2358-4568 1 ACT Venta $ 130.00 $ 130.00 0 02 $ 130.00 002 Tasa 0.160000$ 20.80
01010101 17-2358-4569 1 ACT Venta $ 359.98 $ 359.98 0 02 $ 359.98 002 Tasa 0.160000$ 57.60
01010101 17-2358-4624 1 ACT Venta $ 355.00 $ 355.00 0 02 $ 355.00 002 Tasa 0.160000$ 56.80
Subtotal $844.98
Descuento $135.20$0.00
Total $980.18
Sello digital del Emisor:
Sello digital del SAT:
AAAca0c2-3ea4-4506-abe3-f05771737e69
Fecha y Hora del Certificado:
Total de impuestos trasladados:
**Comprobante Fiscal Digital por Internet**
04 Tarjeta de créditoPUE Pago en una sola exhibición
**Folio interno: LNL023**
2022-06-11 T10:32:08
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
Cadena original del complemento de certificación digital del SAT:
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-06-11T10:32:08|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||


El contribuyente La Linterna S.A. de C.V., generó un CFDI de egreso por el importe

de la operación documentada en el comprobante simplificado con el número 17-

2358 - 4569, y lo relacionó al CFDI global que integró la operación.

```
c) Emisión CFDI de egresos por la operación que se disminuirá para emitir un CFDI
nominativo
```
```
Folio:
Fecha:
Posición:
Terminal:
Responsable: Juan Mirelles R.
Cantidad Modelo Color/Talla Importe IVA
1 365B 30 $ 179.99$ 28.80
1 365B 30 $ 179.99$ 28.80
Subtotal $ 359.98
IVA $ 57.60
TOTAL $ 417.58
Tarjeta de Crédito $ 417.58
Cta 1582
```
```
Ticket: 17-2358-4569
```
```
La Linterna S.A de C.V
RFC: LIN820625PU5
Régimen General de Ley PM
Av. Estrella No.236; Col. Miraluz C.P. 33146
Delegación Cuajimalpa
Tels. (06) 89357925,89357926
********************************************
22147
11/06/2022
2
2
********************************************
```
```
********************************************
```
```
Nombre del Emisor:RFC Emisor: LA LINTERNALIN820625PU5
Clave de Regimen Fiscal:Forma Pago: 601,General de Ley Personas Morales
Método pago:Tipo de comprobante: E Egreso Moneda : MXN
```
**Lugar de expedición:Fecha y Hora de expedición:** (^33146) 2022-06-20 T13:15:11
**Nombre del Receptor:RFC Receptor:** PUBLICO EN GENERALXAXX010101000 **Tipo Relación: 01 Nota de crédito de los documentos relacionados
Código Postal del Receptor:Régimen Fiscal del Receptor:** (^33146) 616, Sin obligaciones fiscales **CFDI relacionado: AAAca0c2-3ea4-4506-abe3-f05771737e69
Uso del CFDI:** G02 Devoluciones, descuentos o bonificaciones.
**Clav Prod. Serv. No Identificación Cantidad UnidadClave Unidad DescripciónValor Unit Importe Descuento Impuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe**
84111506 17-2358-4569 1 ACT Blusa de mujer $ 359.98 $ 359.98 0 02 $ 359.98 002 Tasa 0.160000 $ 57.60
**SubtotalDescuento** $359.98$0.00
**Total** $417.58$57.60
**Sello digital del Emisor:
Sello digital del SAT:**
BBBca0c3-4iu4-5899-abe3-f05771737e70
**Fecha y Hora del Certificado:** 2022-06-20 T13:15:11
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChCTwf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
**Cadena original del complemento de certificación digital del SAT:**
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-06-20T13:15:11|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
**Comprobante Fiscal Digital por Internet Folio interno: LNL037**
04 Tarjeta de créditoPUE Pago en una sola exhibición
**Total de impuestos trasladados:**
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM


```
d) Emisión de CFDI nominativo
```
**Nombre del Emisor:** LA LINTERNA
**RFC Emisor:** LIN820625PU5
**Clave de Regimen Fiscal:** 601,General de Ley Personas Morales
**Forma Pago:
Método pago: Moneda** : MXN
**Tipo de comprobante:** I Ingreso
**Lugar de expedición:** 33146
**Fecha y Hora de expedición:** 2022-06-20 T14:32:08

Nombre del Receptor: MARKUZ KURI
**RFC Receptor:** MKIZ910508JUM **Tipo Relación:
Código Postal del Receptor:** 33147 **CFDI relacionado:
Régimen Fiscal del Receptor:** 612, Personas Físicas con Actividades Empresariales y Profesionales
**Uso del CFDI:** G03 Gastos en general

```
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Descripción Valor Unit Importe Descuento Impuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe
53101604 17-2358-4569 2 H87 Pieza Blusa de mujer $ 359.98 $ 359.98 0 02 $ 359.98 002 Tasa 0.160000 $ 57.60
```
**Subtotal** $359.98
**Descuento** $0.00
$57.60
**Total** $417.58

**Sello digital del Emisor:**

**Sello digital del SAT:**

CCCa0c2-3ea4-9856-abe3-f05771737e40

**Fecha y Hora del Certificado:**

**Comprobante Fiscal Digital por Internet Folio interno: LNL080**

**Total de impuestos trasladados:**

```
01 Efectivo
PUE Pago en una sola exhibición
```
```
2022-06-20 T14:32:08
```
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=

dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=

**Cadena original del complemento de certificación digital del SAT:**

||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-06-20T14:32:08|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||


**_Apéndice 3 Instrucciones específicas de llenado en el CFDI global aplicable_**

**_a Hidrocarburos y Petrolíferos_**

**Vigencia:** _Las disposiciones contenidas en el presente apéndice serán de_

_aplicación opcional a partir del 1 3 de enero de 2020, volviéndose de aplicación_

_obligatoria a partir del 1 de abril del mismo año._

En el apéndice 2 de la presente Guía se encuentra un ejemplo en el que se

describe como se puede aplicar un CFDI de egreso por un descuento, devolución

o bonificación de una operación que fue documentada en un CFDI global.

Al respecto, cabe señalar que para los efectos del CFDI global aplicable a

Hidrocarburos y Petrolíferos y siempre que se haya realizado la venta, _no se_

_deberán cancelar_ los CFDI globales, la única forma de poder realizar la

cancelación de una operación que se llevó a cabo y que esté contenida en un

CFDI global, es la emisión de un CFDI de egreso relacionado por una devolución

de un concepto, en los términos descritos en el citado apéndice 2, para efectos

de la generación del CFDI de ingresos de manera individual y nominativa, en caso

de que un cliente lo haya solicitado.

Los nodos y campos no mencionados en este procedimiento, se deben registrar

en el comprobante fiscal conforme a las especificaciones generales contenidas

en esta guía.

```
FormaPago Se debe registrar la clave de forma de pago con la que se
liquidó el comprobante de operaciones con el público en
general.
```
```
En el caso de existir comprobantes de operaciones con el
público en general con distintas formas de pago, se debe
realizar un CFDI global por cada una de éstas.
```
```
Las diferentes claves de forma de pago se encuentran incluidas
en el catálogo c_FormaPago.
```
```
Ejemplo:
```
```
FormaPago= 02
```
```
c_FormaPago Descripción
```
```
01 Efectivo
```

```
02 Cheque nominativo
```
#### 03

```
Transferencia
electrónica de
fondos
```
**Nodo:Concepto** En este nodo se debe expresar la información detallada de

```
cada uno de los comprobantes de operaciones con el público
en general.
```
ClaveProdServ En este campo se debe registrar la clave del catálogo

```
c_ClaveProdServ publicado en el Portal del SAT,
correspondiente al producto de que se trate:
```
```
 “15101505” en caso de ser Diésel.
 “ 15101514 ” en caso de ser Gasolina regular menor a 91
octanos.
 “ 15101515 ” en caso de ser Gasolina premium mayor o
igual a 91 octanos.
 “15111510” en caso de ser Gas licuado de petróleo.
 “ 15111512 ” en caso de ser Gas natural.
 “15121500” en caso de lubricantes, aceites y otros
productos relacionados al giro del contribuyente.
```
```
Ejemplo:
```
```
ClaveProdServ= 15101505
```
NoIdentificacion En este campo se debe registrar el número de permiso

```
otorgado por la Comisión Reguladora de Energía, seguido de
un guion medio para registrar un número único y consecutivo
por manguera, mismo que puede tener un máximo de 40
caracteres.
```
```
Los permisos cuya nomenclatura se deben registrar son los
siguientes:
```
```
 Expendio en aeródromos - (Con nomenclatura tipo:
PL/XXXXX/EXP/AE/AAAA)
```

```
 Expendio al público de gasolinas y diésel mediante
estación de servicio - (Con nomenclatura tipo:
PL/XXXXX/EXP/ES/AAAA)
```
```
 Expendio al público de gasolinas y diésel mediante
estación de servicio multimodal - (Con nomenclatura tipo:
PL/XXXXX/EXP/ES/MM/AAAA)
```
```
 Expendio al público de gas natural mediante estación de
servicio con fin específico - (Con nomenclatura tipo:
G/XXXXX/EXP/ES/FE/AAAA)
```
```
 Expendio al público de gas natural mediante estación de
servicio multimodal - (Con nomenclatura tipo:
G/XXXXX/EXP/ES/MM/AAAA)
```
```
 Expendio al Público de Gas Licuado de Petróleo
mediante Estación de Servicio con fin Específico - (Con
nomenclatura tipo: LP/XXXXX/EXP/ES/AAAA)
```
```
 Distribución de Gas Licuado de Petróleo por medio de
Auto-Tanques - (Con nomenclatura tipo:
LP/XXXXX/DIST/AUT/AAAA).
```
```
 Distribución de Gas Licuado de Petróleo mediante
Planta de Distribución - (Con nomenclatura tipo:
LP/XXXXX/DIST/PLA/AAAA)
```
```
 Distribución de Gas Licuado de Petróleo por medio de
vehículos de reparto (Con nomenclatura tipo:
LP/XXXXX/DIST/REP/AAAA)
```
```
 Distribución de gas natural por medio de ductos - (Con
nomenclatura tipo: G/XXXXX/DIS/AAAA)
```
```
 Distribución de gas natural por medios distintos a
ductos - (Con nomenclatura tipo: G/XXXXX/DIS/OM/AAAA)
```
**Ejemplo A:**

Para expendio al público de gasolinas y diésel, mediante

estación de servicio, se generaría el siguiente número de

identificación:


```
NoIdentificacion = PL/00320/EXP/ES/2019- 1
```
```
Ejemplo B:
```
```
Para expendio al público de Gas Licuado de Petróleo, mediante
estación de servicio con fin específico, se generaría el siguiente
número de identificación:
```
```
NoIdentificacion = LP/00751/EXP/ES/2016- 105012365
```
```
Si el tipo de comprobante es de Egreso, en este campo se debe
expresar la información del campo NoIdentificacion del
comprobante fiscal que se está relacionado en este CFDI.
```
```
Nota: Cuando se haya registrado la clave “15121500”, se debe
registrar el número de folio o de operación de los
comprobantes de operación con el público en general, mismo
que puede tener un máximo de 40 caracteres.
```
Cantidad En este campo se debe registrar la cantidad de litros,

```
tratándose de Diésel, Gasolina, Gas Licuado de Petróleo o
metros cúbicos, tratándose de Gas Natural que correspondan
a cada concepto.
```
```
Nota: Cuando se haya realizado la medición en una unidad de
medida distinta, deberá realizarse la conversión
correspondiente.
```
ClaveUnidad Se debe registrar la clave “LTR”, tratándose de Diésel, Gasolina,

```
Gas Licuado de Petróleo y la clave “MTQ”, tratándose de Gas
Natural, del catálogo c_ClaveUnidad publicado en el Portal del
SAT.
```
```
Nota: Cuando se haya realizado la medición en una unidad de
medida distinta, deberá realizarse la conversión
correspondiente.
```
```
Ejemplo:
```
```
ClaveUnidad= LTR
```
Descripcion En este campo se debe registrar la descripción del producto de

```
que se trate.
```

ValorUnitario En este campo se debe registrar el valor o precio unitario del

```
bien por cada concepto, el cual puede contener de cero hasta
seis decimales.
```
```
Si el tipo de comprobante es de “I” (Ingreso) o “E” (Egreso), este
valor debe ser mayor a cero.
```
```
Ejemplo:
```
```
ValorUnitario= 1230.00
```
**Nodo:**

**Impuestos**

```
En este nodo se debe expresar los impuestos aplicables a cada
concepto.
```
```
Si se registra información en este nodo, debe existir la sección:
traslados.
```
**Nodo:Traslados** En este nodo se debe expresar los impuestos trasladados

```
aplicables a cada concepto.
```
**Nodo:Traslado** En este nodo se debe expresar la información detallada de un

```
traslado de impuestos aplicable a cada concepto.
```
```
En el caso de que un concepto contenga impuesto trasladado
por Tasa y Cuota, se debe expresar en diferentes apartados.
```
```
Nota: Debe considerarse que estos comprobantes no tienen
un receptor en realidad que pudiera usarlos para
acreditamiento, por lo que los desgloses de impuestos
trasladados tienen el fin de dar control y fácil identificación de
estos datos a su emisor.
```

Impuesto Se debe registrar la clave del tipo de impuesto trasladado

```
aplicable a cada concepto, las cuales se encuentran incluidas
en el catálogo c_Impuesto publicado en el Portal del SAT.
```
```
Ejemplo:
```
```
Impuesto= 002
```
```
c_Impuesto Descripción
```
```
002 IVA
```
```
003 IEPS
```
TasaOCuota Se debe registrar el valor de la tasa o cuota del impuesto que

```
se traslada para cada concepto. Es requerido cuando el campo
TipoFactor corresponda a Tasa o Cuota.
```
```
 Si el valor registrado es fijo debe corresponder al tipo de
impuesto y al tipo de factor conforme al catálogo
c_TasaOCuota.
```
```
 Si el valor registrado es variable, debe corresponder al
rango entre el valor mínimo y el valor máximo señalado
en el catálogo.
```
```
Ejemplo:
```
```
TasaOCuota= 0.160000
```
```
Rango o Fijo
```
```
c_TasaOCuota
Impuesto Factor
Valor mínimo Valor máximo
```
```
Fijo No 0.000000 IVA Tasa
```
```
Fijo No 0.160000 IVA Tasa
```
Importe Se debe registrar el importe del impuesto trasladado que

```
aplica a cada concepto. No se permiten valores negativos. Este
campo es requerido cuando en el campo TipoFactor se haya
registrado como Tasa o Cuota.
```

El valor de este campo será calculado por el sistema que

genera el comprobante y considerará los redondeos que tenga

registrado éste campo en el estándar técnico del Anexo 20,

para mayor referencia puede consultar la documentación

técnica publicada en el Portal del SAT.

Este campo debe contener de cero hasta seis decimales.


### Control de cambios a la Guía de llenado del CFDI global Versión 4.0 del

### CFDI

```
Guía publicada en el Portal del SAT en Internet el 31 de diciembre de 20 21
```
```
Revisión Fecha de
actualización
```
```
Descripción de la modificación o incorporación
```
```
1 08 de marzo
del 202 3.
```
```
 Se precisa redacción en la Introducción.
 Se precisa fundamento legal en el campo
LugarExpedicion.
 Se precisa redacción en los campos:
TipoDeComprobante, Meses y
RegimenFiscalReceptor.
 Se precisa redacción a las disposiciones Generales
del Apéndice 2 “Caso de Uso de CFDI de egreso
aplicable a un CFDI global”.
 Se ajusta el inciso b), se eliminan los incisos C1 y d)
de la fracción I. Ejemplo de CFDI Global con CFDI
de egreso relacionado por una devolución de un
concepto del Apéndice 2 “Caso de Uso de CFDI de
egreso aplicable a un CFDI global”.
 Se adiciona la fracción III al Apéndice 2 “Caso de
Uso de CFDI de egreso aplicable a un CFDI global”.
 Se actualizan los permisos cuya nomenclatura se
deben registrar, dentro del atributo
NoIdentificacion del Apéndice 3 “Instrucciones
específicas de llenado en el CFDI global aplicable
a Hidrocarburos y Petrolíferos”.
```

