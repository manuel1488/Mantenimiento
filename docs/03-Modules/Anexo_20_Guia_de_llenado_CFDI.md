# Anexo 20

# Guía de llenado de los

# comprobantes fiscales

# digitales por Internet


```
Contenido
```
 **_Introducción_** ..................................................................................................................... 3

 **_I. Guía de llenado del Comprobante Fiscal Digital por Internet (CFDI)_** 5

 **_II. Guía de llenado del Comprobante Fiscal Digital por Internet que_**

```
ampara retenciones e información de pagos ....................................................... 41
```
 **_Glosario_** .............................................................................................................................................. 54

 **_Apéndice 1 Notas Generales_** ................................................................................................ 55

 **_Apéndice 2 Clasificación de los tipos de CFDI_** ...................................................... 56

 **_Apéndice 3 Clasificación de Productos y Servicios_** ............................................ 57

 **_Apéndice 4 Catálogos del comprobante_** .................................................................. 63

 **_Apéndice 5 Emisión de CFDI de Egresos_** ................................................................... 64

 **_Apéndice 6 Procedimiento para la emisión de los CFDI en el caso de_**

```
anticipos recibidos .................................................................................................................... 69
```
 **_Apéndice 7 Preguntas y respuestas sobre el Anexo 20 versión 4.0_** ..... 76

 **_Apéndice 8 Caso de Uso Facturación de Anticipos_** .......................................... 90

 **_Apéndice 9 Caso de Uso Facturación por contratos de obra pública_** 94

 **_Apéndice 10 Caso de Uso Emisión del CFDI por donativos otorgados en_**

```
numerario o en especie y donativos globales en numerario o en
especie .............................................................................................................................................. 104
```
 **_Apéndice 11 Instrucciones específicas de llenado en el CFDI aplicable a_**

```
operaciones individuales a Hidrocarburos, Petrolíferos y Servicios
relacionados ................................................................................................................................. 108
```
 **_Control de cambios de la Guía de llenado del Comprobante Fiscal_**

```
Digital por Internet (CFDI) ................................................................................................... 119
```
 **_Control de cambios de la Guía de llenado del Comprobante Fiscal_**

```
Digital por Internet que ampara retenciones e información de pagos
................................................................................................................................................................. 120
```

```
Introducción
```
Los Comprobantes Fiscales Digitales por Internet (CFDI) deben emitirse por los
actos o actividades que se realicen, por los ingresos que perciban o por las
retenciones de contribuciones que efectúen los contribuyentes ya sean
personas físicas o morales.

El artículo 29-A del Código Fiscal de la Federación (CFF) establece los requisitos

que deben contener los CFDI, en relación con lo establecido en el artículo 29,

segundo párrafo, fracción VI del citado Código, dichos comprobantes deben

cumplir con las especificaciones que, en materia de informática, determine el

Servicio de Administración Tributaria (SAT), mediante reglas de carácter general.

Expedir CFDI es una obligación de los contribuyentes personas físicas o morales

de conformidad con los artículos 29, párrafos primero y segundo, fracción IV y

penúltimo párrafo del CFF y 39 del Reglamento del CFF, en relación con la regla

2.7.5.4., y el Capítulo 2.7. “De los Comprobantes Fiscales Digitales por Internet o

Factura Electrónica” de la Resolución Miscelánea Fiscal vigente.

Los documentos técnicos especifican la estructura, forma y sintaxis que deben
contener los CFDI que expidan los contribuyentes, lo cual permite que la
información se integre de manera organizada en el comprobante, y harán
referencia a la versión 4.0.

En la sección I de este documento se describe cómo se debe realizar el llenado

de los datos a registrar en el CFDI y en la sección II el CFDI que ampara

retenciones e información de pagos.

En el caso de alguna duda o situación particular sobre el llenado del

comprobante que no se encuentre resuelta en esta guía, el contribuyente debe

remitirse a los siguientes documentos, mismos que se encuentran publicados

en el Portal del SAT:

```
 Documentación técnica.
 Preguntas y respuestas de los comprobantes fiscales digitales por
Internet.
 Preguntas y respuestas del comprobante fiscal digital por Internet que
ampara retenciones e información de pagos.
 Casos de uso de los comprobantes fiscales digitales por Internet.
 Casos de uso del comprobante fiscal digital por Internet que ampara
retenciones e información de pagos.
```

La presente guía de llenado es un documento cuyo objeto es explicar a los

contribuyentes la forma correcta de llenar y expedir un CFDI, al observar las

definiciones del estándar tecnológico del Anexo 20 y las disposiciones jurídicas

vigentes aplicables, para ello se hace uso de ejemplos que faciliten las

explicaciones, por ello es importante aclarar que los datos usados para los

ejemplos son ficticios para efectos didácticos a fin de explicar de manera fácil

cómo se llena un CFDI.

Por lo anteriormente señalado, el lector debe tener claro que las explicaciones

realizadas en esta Guía de llenado no sustituyen a las disposiciones fiscales

legales o reglamentarias vigentes, por lo que en temas distintos a la forma

correcta de llenar y expedir un CFDI, como pueden ser los relativos a la

determinación de las contribuciones, los sujetos, el objeto, las tasas, las tarifas, las

mecánicas de cálculo, los requisitos de las deducciones, entre otros, los

contribuyentes deberán observar las disposiciones fiscales vigentes aplicables.


**_I. Guía de llenado del Comprobante Fiscal Digital por Internet (CFDI)_**

```
Cuando se emita un CFDI, se debe realizar con las especificaciones señaladas en
cada uno de los campos expresados en lenguaje no informático que se incluyen
en esta sección.
```
```
En el presente documento se hace referencia a la descripción de la información
que debe contener el citado comprobante fiscal.
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
Version
Debe tener el valor “4.0”.
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

Sello Es el sello digital del comprobante fiscal generado con el
certificado de sello digital del contribuyente emisor del
comprobante; éste funge como la firma del emisor del
comprobante y lo integra el sistema que utiliza el contribuyente
para la emisión del comprobante.

FormaPago Se debe registrar la clave de la forma de pago de los bienes, la
prestación de los servicios, el otorgamiento del uso o goce, o la
forma en que se recibe el donativo contenidos en el
comprobante.

```
 En el caso de que se haya recibido el pago de la
contraprestación al momento de la emisión del
comprobante fiscal, los contribuyentes deberán
consignar en éste la clave vigente correspondiente a la
forma en que se recibió el pago de conformidad con el
catálogo c_FormaPago publicado en el Portal del SAT.
```
```
En este supuesto no se debe emitir adicionalmente un
CFDI al que se le incorpore el “Complemento para
recepción de pagos”, porque el comprobante ya está
pagado.
```
```
 En el caso de aplicar más de una forma de pago en una
transacción, los contribuyentes deben incluir en este
campo la clave vigente del catálogo c_FormaPago de la
forma de pago con la que se liquida la mayor cantidad
del pago. En caso de que se reciban distintas formas de
pago con el mismo importe, el contribuyente debe
registrar a su consideración una de las formas de pago
con las que se recibió el pago de la contraprestación.
 En el caso de que no se reciba el pago de la
contraprestación al momento de la emisión del
comprobante fiscal (pago en parcialidades o diferido), los
contribuyentes deberán seleccionar la clave “99” (Por
definir) del catálogo c_FormaPago publicado en el Portal
del SAT.
```
```
En este supuesto la clave del método de pago debe ser
“PPD” (Pago en parcialidades o diferido) y cuando se
reciba el pago total o parcial se debe emitir
adicionalmente un CFDI al que se le incorpore el
“Complemento para recepción de pagos” por cada pago
que se reciba.
En el caso de donativos entregados en especie, en este campo
se debe registrar la clave “12” (Dación en pago).
```

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
03 Transferencia electrónica de fondos
```
```
99 Por definir
```
(^)
Cuando el tipo de comprobante sea “E” (Egreso), se deberá
registrar como forma de pago la misma clave vigente que se
registró en el CFDI “I” (Ingreso) que dio origen a este
comprobante, derivado ya sea de una devolución, descuento o
bonificación, conforme al catálogo de formas de pago del
Anexo 20, opcionalmente se podrá registrar la clave vigente de
forma de pago con la que se está efectuando el descuento,
devolución o bonificación en su caso.
**Ejemplo:** Un contribuyente realiza la compra de un producto
por un valor de $1,000.00, y se le emite un CFDI de tipo “I”
(Ingreso). La compra se pagó con forma de pago “01” (Efectivo),
posteriormente, éste realiza la devolución de dicho producto,
por lo que el contribuyente emisor del comprobante debe
emitir un CFDI de tipo “E” (Egreso) por dicha devolución,
registrar la forma de pago “01” (Efectivo), puesto que esta es la
forma de pago registrada en el CFDI tipo “I” (Ingreso) que se
generó en la operación de origen.
FormaPago= **01**
NoCertificado Es el número que identifica al certificado de sello digital del
emisor, el cual lo incluye en el comprobante fiscal el sistema
que utiliza el contribuyente para la emisión.
Certificado Es el contenido del certificado del sello digital del emisor y lo
integra el sistema que utiliza el contribuyente para la emisión
del comprobante fiscal.


CondicionesDe
Pago

```
Se pueden registrar las condiciones comerciales aplicables para
el pago del comprobante fiscal, cuando existan éstas y cuando
el tipo de comprobante sea “I” (Ingreso) o “E” (Egreso).
```
```
En este campo se podrán registrar de uno hasta 1000
caracteres.
```
```
Ejemplo:
CondicionesDePago= 3 meses
```
SubTotal Es la suma de los importes de los conceptos antes de
descuentos e impuestos. No se permiten valores negativos.

```
 Este campo debe tener hasta la cantidad de decimales
que soporte la moneda, ver ejemplo del campo Moneda.
```
```
 Cuando en el campo TipoDeComprobante sea “I”
(Ingreso), “E” (Egreso) o “N” (Nómina), el importe
registrado en este campo debe ser igual al redondeo de
la suma de los importes de los conceptos registrados.
 Cuando en el campo TipoDeComprobante sea “T”
(Traslado) o “P” (Pago) el importe registrado en este
campo debe ser igual a cero.
```
Descuento Se puede registrar el importe total de los descuentos aplicables
antes de impuestos. No se permiten valores negativos. Se debe
registrar cuando existan conceptos con descuento.

```
 Este campo debe tener hasta la cantidad de decimales
que soporte la moneda, ver ejemplo del campo Moneda.
 El valor registrado en este campo debe ser menor o igual
que el campo Subtotal.
```
```
 Cuando en el campo TipoDeComprobante sea “I”
(Ingreso), “E” (Egreso) o “N” (Nómina) y algún concepto
incluya un descuento, este campo debe existir y debe ser
igual al redondeo de la suma de los campos Descuento
registrados en los conceptos; en otro caso se debe omitir
este campo.
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
TipoCambio Se puede registrar el tipo de cambio FIX conforme a la moneda
registrada en el comprobante.

```
Este campo es requerido cuando la clave de moneda es distinta
de “MXN” (Peso Mexicano) y a la clave “XXX” (Los códigos
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
```
```
Esta validación estará vigente únicamente a partir de que el
SAT publique en su Portal los procedimientos para generar
```

```
la clave de confirmación y parametrizar los rangos máximos
aplicables.
```
(^)
Total Es la suma del subtotal, menos los descuentos aplicables, más
las contribuciones recibidas (impuestos trasladados federales o
locales, derechos, productos, aprovechamientos, aportaciones
de seguridad social, contribuciones de mejoras) menos los
impuestos retenidos federales y/o locales. No se permiten
valores negativos.
 Este campo debe tener hasta la cantidad de decimales
que soporte la moneda, ver ejemplo del campo Moneda.
 Cuando el campo TipoDeComprobante sea “T” (Traslado)
o “P” (Pago), el importe registrado en este campo debe
ser igual a cero.
 El SAT publica el límite para el valor máximo de este
campo en:
 El catálogo c_TipoDeComprobante.
 En la lista de RFC (l_RFC) cuando el contribuyente
registre en el Portal del SAT los límites
personalizados.
 Cuando el valor equivalente en “MXN” (Peso Mexicano)
de este campo exceda el límite establecido, debe existir
el campo Confirmacion.
**Nota importante:
Esta validación estará vigente únicamente a partir de que el
SAT publique en su Portal los procedimientos para generar
la clave de confirmación y para parametrizar los montos
máximos aplicables.**
TipoDeCompro
bante
Se debe registrar la clave con la que se identifica el tipo de
comprobante fiscal para el contribuyente emisor.
**Ejemplo:**
TipoDeComprobante= **I**
Los distintos tipos de comprobante se encuentran incluidos en
el catálogo c_TipoDeComprobante, adicionalmente se podrán
consultar en el Apéndice 2 “Clasificación de los tipos de CFDI”
de esta guía.


```
 No debe existir el campo CondicionesDePago cuando el
campo TipoDeComprobante es “T” (Traslado), “P” (Pago)
o “N” (Nómina).
```
```
 No debe existir el campo Descuento de los conceptos
cuando el campo TipoDeComprobante es “T” (Traslado) o
“P” (Pago).
 No debe existir el nodo Impuestos cuando el campo
TipoDeComprobante es “T” (Traslado), “P” (Pago) o “N”
(Nómina).
```
```
 No debe existir el campo FormaPago cuando el campo
TipoDeComprobante es “N” (Nómina).
 No deben existir los campos FormaPago y MetodoPago
cuando el campo TipoDeComprobante es “T” (Traslado) o
“P” (Pago).

```
Exportacion
Se debe registrar la clave con la que se identifica si el
comprobante ampara una operación de exportación, las
distintas claves vigentes se encuentran incluidas en el catálogo
c_Exportacion.

```
 Cuando se registre el valor “02”, se debe incluir el
“Complemento para Comercio Exterior”.
```
```
Ejemplo:
```
```
Exportacion= 01
```
```
c_Exportacion Descripción
01 No aplica
```
MetodoPago
Se debe registrar la clave que corresponda depende si se paga
en una sola exhibición o en parcialidades, las distintas claves de
método de pago se encuentran incluidas en el catálogo
c_MetodoPago.

```
Ejemplo: Si un contribuyente realiza el pago en una sola
exhibición debe registrar en el campo de método de pago lo
siguiente:
MetodoPago = PUE
```
```
c_MetodoPago Descripción
PUE Pago en una sola exhibición
```

```
PPD Pago en parcialidades o diferido
```
```
Se debe registrar la clave “PUE” (Pago en una sola exhibición),
cuando se realice dicho pago al momento de emitir el
comprobante.
```
```
Se debe registrar la clave “PPD” (Pago en parcialidades o
diferido), cuando se emita el comprobante de la operación y con
posterioridad se vaya a liquidar en un solo pago el saldo total o
en varias parcialidades. En caso de que al momento de la
operación se realice el pago de la primera parcialidad, se debe
emitir el comprobante por el monto total de la operación y un
segundo comprobante con el Complemento para recepción de
Pagos por la parcialidad.
```
LugarExpedicio
n

```
Se debe registrar el código postal del lugar de expedición del
comprobante (domicilio de la matriz o de la sucursal), debe
corresponder a una clave de código postal vigente incluida en
el catálogo c_CodigoPostal.
```
```
Al ingresar el código postal en este campo se cumple con el
requisito de señalar el domicilio y lugar de expedición del
comprobante a que se refieren las fracciones I y III del primer
párrafo del artículo 29-A del CFF, en los términos de la regla
2.7.1. 29 ., fracción I de la Resolución Miscelánea Fiscal vigente.
```
```
En el caso de que se emita un comprobante fiscal en una
sucursal, en dicho comprobante se debe registrar el código
postal de ésta, independientemente de que los sistemas de
facturación de la empresa se encuentren en un domicilio
distinto al de la sucursal.
```
```
Los distintos códigos postales se encuentran incluidos en el
catálogo c_CodigoPostal.
```
```
Ejemplo:
LugarExpedicion= 01000
```
```
c_CodigoPostal
01000
```
Confirmacion
Se debe registrar la clave de confirmación única e irrepetible
que entrega el proveedor de certificación de CFDI o el SAT a los
emisores (usuarios) para expedir el comprobante con importes
o tipo de cambio fuera del rango establecido o en ambos casos.


```
Ejemplo:
Confirmacion= ECVH
```
```
Se deben registrar valores alfanuméricos de cinco posiciones.
```
```
Nota importante:
```
```
El uso de esta clave estará vigente únicamente a partir de
que el SAT publique en su Portal los procedimientos para
generar la clave de confirmación y parametrizar los montos
y rangos máximos aplicables.
```
**Nodo:
InformacionGl
obal**

```
En este nodo se puede expresar la información relacionada con
el comprobante global de operaciones con el público en
general.
```
```
Nota: Las especificaciones del llenado de este nodo y sus
campos (Periodicidad, Meses y Año), se encuentran
contenidas en la “Guía de llenado del CFDI Global”.
```
**Nodo:
CfdiRelacionad
os**

```
En este nodo se puede expresar la información de los
comprobantes fiscales relacionados.
```
TipoRelacion Se debe registrar la clave de la relación que existe entre este
comprobante que se está generando y el o los CFDI previos.

```
Las diferentes claves de Tipo de relación se encuentran
incluidas en el catálogo c_TipoRelacion publicado en el Portal
del SAT.
 Cuando el tipo de relación tenga la clave “01” o “02”, no se
deben registrar notas de crédito y débito con
comprobante de tipo “T” (Traslado), “P” (Pago) o “N”
(Nómina).
 Cuando el tipo de relación tenga la clave “03”, no se
deben registrar devoluciones de mercancías sobre
comprobantes de tipo “E” (Egreso), “P” (Pago) o “N”
(Nómina).
 Cuando el tipo de relación tenga la clave “04”, si este
documento que se está generando es de tipo “I” (Ingreso)
o “E” (Egreso), puede sustituir a un comprobante de tipo
“I” (Ingreso) o “E” (Egreso), en otro caso debe de sustituir
a un comprobante del mismo tipo.
```
```
 Cuando el tipo de relación sea “05”, este documento que
se está generando debe ser de tipo “T” (Traslado), y los
```

```
documentos relacionados deben ser un comprobante de
tipo “I” (Ingreso) o “E” (Egreso).
```
```
 Cuando el tipo de relación sea “06”, este documento que
se está generando debe ser de tipo “I” (Ingreso) o “E”
(Egreso) y los documentos relacionados deben ser de
tipo “T” (Traslado).
```
```
 Cuando el tipo de relación sea “07”, este documento que
se está generando debe ser de tipo “I” (Ingreso) o “E”
(Egreso) y los documentos relacionados deben ser de
tipo “I” (Ingreso) o “E” (Egreso).
```
```
Ejemplo:
TipoRelacion= 01
```
```
c_TipoRelacion Descripción
01 Nota de crédito de los documentos
relacionados
02 Nota de débito de los documentos
relacionados
03 Devolución de mercancía sobre facturas o
traslados previos
04 Sustitución de los CFDI previos
05 Traslados de mercancías facturados
previamente
06 Factura generada por los traslados previos
07 CFDI por aplicación de anticipo
```
(^)
(^)
**Nodo:
CfdiRelacionad
o**
En este nodo se debe expresar la información de los
comprobantes fiscales relacionados con el que se está
generando, se deben expresar tantos números de nodos de
CfdiRelacionado como comprobantes se requieran relacionar.
UUID Se debe registrar el folio fiscal (UUID) de un comprobante fiscal
relacionado con el presente comprobante.
**Ejemplo:**
UUID= **5FB2822E-396D- 4725 - 8521 - CDC4BDD20CCF
Nodo: Emisor** En este nodo se debe expresar la información del contribuyente
que emite el comprobante fiscal.
Rfc Se debe registrar la clave del Registro Federal de
Contribuyentes del emisor del comprobante.


```
En el caso de que el emisor sea una persona física, este campo
debe contener una longitud de 13 posiciones, si se trata de
personas morales debe contener una longitud de 12 posiciones.
```
```
Ejemplo:
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
El nombre debe corresponder a la clave de RFC registrado en el
campo Rfc de este Nodo.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
Nombre = MARTON ALEEJANDRO SANZI FIERROR
```
```
En el caso de una persona moral se debe registrar:
Nombre = LA PALMA AEI
```
RegimenFiscal
Se debe especificar la clave vigente del régimen fiscal del
contribuyente emisor bajo el cual se está emitiendo el
comprobante.

```
Las claves de los diversos regímenes se encuentran incluidas en
el catálogo c_RegimenFiscal publicado en el Portal del SAT.
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
No Si
```
```
603 Personas Morales
con Fines no
Lucrativos
```
```
No Si
```
```
605 Sueldos y Salarios
e Ingresos
Asimilados a
Salarios
```
```
Si No
```
FacAtrAdquiren
te

```
Se debe registrar el número de operación proporcionado por el
SAT cuando se trate de un comprobante a través del adquirente
de los productos o servicios siempre que la respuesta del
servicio sea en sentido positivo, conforme a la Resolución
Miscelánea Fiscal vigente.
```
```
Cuando el número de operación sea menor a una longitud de
10 posiciones, se deberá completar con ceros a la izquierda
hasta completar 10 dígitos.
```
```
Ejemplo:
```
```
FacAtrAdquirente= 00000012345
```
(^)
**Nodo:
Receptor**
En este nodo se debe expresar la información del contribuyente
receptor del comprobante.
Rfc Se debe registrar la clave del Registro Federal de
Contribuyentes del receptor del comprobante.
 El RFC debe estar contenido en la lista de RFC (l_RFC)
inscritos no cancelados en el SAT en caso de que sea
diferente del RFC genérico.
**Ejemplo:** En el caso de que el receptor sea una persona física el
RFC debe tener una longitud de 13 posiciones, si se trata de
personas morales debe tener una longitud de 12 posiciones.
Persona física
Rfc= **FIMA420127R**


```
Persona moral
Rfc= COR391215F4A
```
Nombre
Se debe registrar el(los) nombre(s), primer apellido, segundo
apellido, según corresponda denominación o razón social
registrados en el RFC del contribuyente receptor del
comprobante.

```
El Nombre debe corresponder a la clave de RFC registrado en
el campo Rfc de este Nodo.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
Nombre = RAFAELI CAMPOSORIO RUÍZO
```
```
En el caso de una persona moral se debe registrar:
Nombre= LA VILLA ESP
```
DomicilioFiscal

Receptor

```
Se debe registrar el código postal del domicilio fiscal del
receptor del comprobante.
```
```
 El código postal, en caso de que sea diferente de los RFC
genéricos, debe estar asociado a la clave de RFC
registrado en el campo Rfc de este Nodo.
```
```
Ejemplo:
DomicilioFiscalReceptor= 01001
```
ResidenciaFisca
l

```
Cuando el receptor del comprobante sea un residente en el
extranjero, se debe registrar la clave del país de residencia para
efectos fiscales del receptor del comprobante.
```
```
Este campo es obligatorio cuando el RFC del receptor es un RFC
genérico extranjero, y se incluya el complemento de comercio
exterior o se registre el campo NumRegIdTrib.
```

```
Ejemplo: Si la residencia fiscal de la empresa extranjera
receptora del comprobante fiscal se encuentra en Estados
Unidos de América, se debe registrar lo siguiente:
```
```
ResidenciaFiscal= USA
```
```
c_Pais Descripción
```
```
USA Estados Unidos (los)
```
NumRegIdTrib
Se captura el número de registro de identidad fiscal del
receptor del comprobante fiscal cuando este sea residente en
el extranjero.

```
 Este campo es obligatorio cuando se incluya el
complemento de comercio exterior.
 Puede conformarse desde uno hasta 40 caracteres.
```
```
 Si no existe el campo ResidenciaFiscal, este campo
puede no existir.
 La residencia fiscal debe corresponder con el valor
especificado en la columna Formato de Registro de
Identidad Tributaria del catálogo c_Pais.
```
```
Ejemplo: En el caso de que el receptor del comprobante fiscal
sea residente en el extranjero se debe registrar conforme a lo
siguiente:
```
```
NumRegIdTrib= 121585958
```
RegimenFiscal
Receptor

```
Se debe registrar la clave vigente del régimen fiscal del
contribuyente receptor.
```
```
 Las claves de los diversos regímenes se encuentran
incluidas en el catálogo c_RegimenFiscal publicado en el
Portal del SAT.
```
```
 Cuando se trate de operaciones con residentes en el
extranjero y se registre el valor “XEXX010101000” en este
campo se debe registrar la clave “616” Sin obligaciones
fiscales.
```

```
Ejemplo: En el caso de que el receptor sea una persona física
inscrita en el Régimen Arrendamiento, debe registrar lo
siguiente:
```
```
RegimenFiscal= 606
```
```
Aplica para tipo persona
```
```
c_RegimenFiscal Descripción Física Moral
```
```
606 Arrendamiento Si No
```
UsoCFDI Se debe registrar la clave que corresponda al uso que le dará al
comprobante fiscal el receptor.
La clave que solicite el receptor (física o moral) se registre en
este campo, debe corresponder con los valores indicados en el
catálogo c_UsoCFDI y el valor registrado en el campo
RegimenFiscalReceptor, debe corresponder a un valor de la
columna Régimen Fiscal Receptor de dicho catálogo.

```
Ejemplo:
UsoCFDI= D
```
```
c_UsoCFDI Descripción Aplica para tipo
persona
```
```
Régimen Fiscal
Receptor
```
```
Física Moral
```
```
D01 Honorarios
médicos,
dentales y gastos
hospitalarios.
```
```
Sí No
```
```
605
```
```
En el caso de que se emita un CFDI a un residente en el
extranjero con RFC genérico (XEXX010101000), en este campo
se debe registrar la clave “S01” (Sin efectos fiscales).
```
(^)


**Nodo:
Conceptos**

```
En este nodo se deben expresar los conceptos descritos en el
comprobante.
```
**Nodo:
Concepto**

```
En este nodo se debe expresar la información detallada de un
bien o servicio descrito en el comprobante.
```
ClaveProdServ En este campo se debe registrar una clave que permita
clasificar los conceptos del comprobante como productos o
servicios; se deben utilizar las claves de los diversos productos o
servicios de conformidad con el catálogo c_ClaveProdServ
publicado en el Portal del SAT, cuando los conceptos que se
registren por sus actividades correspondan a estos.

```
Para una mejor ubicación de los productos y servicios que se
facturan, puede consultarse el Apéndice 3 de esta Guía.
```
```
En el caso de que la clave de un producto o servicio no se
encuentre en el catálogo se debe registrar la clave “01010101”.
```
```
Ejemplo:
ClaveProdServ= 60121001
```
```
c_ClaveProdServ Descripció
n
```
```
Incluir IVA
trasladado
```
```
Incluir IEPS
trasladado
```
```
60121001
Pinturas
Opcional Opcional
```
```
01010101 No existe en el catálogo Opcional Opcional
```
```
Basta con que se clasifique la descripción del bien o servicio
hasta el tercer nivel, es decir hasta la clase, los primeros seis
dígitos de la clave del catálogo (Apéndice 3).
```
```
Es importante señalar que la identificación de la clave de
producto o servicio que corresponda conforme al catálogo
c_ClaveProdServ, será responsabilidad del emisor de la factura,
en razón de ser él quien conoce las características y la
naturaleza del producto o servicio que comercializa y amparará
el comprobante.
```

```
En el caso de que el emisor del comprobante comercialice
productos que no hayan sido objeto de transformación o
industrialización de su parte –es decir lo compra y tal cual lo
vende-, el emisor podrá utilizar la clave del producto registrada
por su proveedor en el comprobante que ampara la adquisición
de los mismos.
```
NoIdentificacio
n

```
En este campo se puede registrar el número de parte,
identificador del producto o del servicio, la clave de producto o
servicio, SKU (número de referencia) o equivalente, propia de la
operación del contribuyente emisor del comprobante fiscal
descrito en el presente concepto.
```
```
 Opcionalmente se pueden utilizar claves del estándar
GTIN (número global de artículo comercial).
```
```
 Puede conformarse desde uno hasta 100 caracteres
alfanuméricos.
```
```
Ejemplo:
NoIdentificacion= UT421510
```
Cantidad En este campo se debe registrar la cantidad de bienes o
servicios que correspondan a cada concepto, puede contener
de cero hasta seis decimales.

```
Ejemplo:
Cantidad= 5.555555
```
ClaveUnidad En este campo se debe registrar la clave de unidad de medida
estandarizada de conformidad con el catálogo c_ClaveUnidad
publicado en el Portal del SAT, aplicable para la cantidad
expresada en cada concepto. La unidad debe corresponder con
la descripción del concepto.

```
Ejemplo:
ClaveUnidad= KGM
```
```
c_ClaveUnidad Nombre Símbolo
```
```
KGM Kilogramo Kg
```
```
SR Tira
```

Unidad En este campo se puede registrar la unidad de medida del bien
o servicio propio de la operación del emisor, aplicable para la
cantidad expresada en cada concepto. La unidad debe
corresponder con la descripción del concepto.

```
La unidad debe corresponder con la ClaveUnidad del catálogo
c_ClaveUnidad.
```
```
Ejemplo:
Unidad= Kilo
```
Descripcion En este campo se debe registrar la descripción del bien o
servicio propio de la empresa por cada concepto.

```
Si se trata de la enajenación de tabacos labrados, en este campo
se debe especificar el peso total de tabaco contenido en los
tabacos labrados enajenados o, en su caso, la cantidad de
cigarros enajenados.
```
```
Si se trata de ventas de primera mano, en este campo se debe
registrar la fecha del documento aduanero, la cual puede ser
con un formato libre, ya sea antes o después de la descripción
del producto.
```
```
Si se trata de importaciones efectuadas a favor de un tercero,
en este campo se debe registrar el número y fecha del
documento aduanero, los conceptos y montos pagados por el
contribuyente directamente al proveedor extranjero y los
importes de las contribuciones pagadas con motivo de la
importación.
```
```
Ejemplo:
Descripcion= Reparación de lavadora
```
```
Puede conformarse desde uno hasta 1000 caracteres
alfanuméricos.
```
ValorUnitario En este campo se debe registrar el valor o precio unitario del
bien o servicio por cada concepto, el cual puede contener de
cero hasta seis decimales.

```
Si el tipo de comprobante es de “I” (Ingreso), “E” (Egreso) o “N”
(Nómina) este valor debe ser mayor a cero, si es de “T” (Traslado)
```

```
puede ser mayor o igual a cero y si es de “P” (Pago) debe ser
igual a cero.
```
```
Ejemplo:
ValorUnitario= 1230.00
```
Importe Se debe registrar el importe total de los bienes o servicios de
cada concepto. Debe ser equivalente al resultado de multiplicar
la cantidad por el valor unitario expresado en el concepto, el
cual debe ser calculado por el sistema que genera el
comprobante y considerará los redondeos que tenga registrado
este campo en el estándar técnico del Anexo 20. No se permiten
valores negativos.

```
Este campo puede contener de cero hasta seis decimales.
```
```
Ejemplo 1: En este caso se consideró la clave “MXN” (Peso
Mexicano).
```
```
Importe= 6150.00
```
```
Cantidad Valor unitario Importe
```
```
5 1230.00 6150.00
```
```
Ejemplo 2: En este caso se consideró la clave “MXN” (Peso
Mexicano).
```
```
Importe= 3864.22827
```
```
Cantidad Valor unitario Importe
```
```
3.141649 1230.00 3864.22827
```
```
Para validar el cálculo del redondeo de este campo puede
consultar la documentación técnica publicada en el Portal del
SAT.
```
Descuento
Se puede registrar el importe de los descuentos aplicables a
cada concepto, debe tener hasta la cantidad de decimales que
tenga registrado en el campo importe del concepto y debe ser
menor o igual al campo Importe. No se permiten valores
negativos.


```
Este campo puede contener de cero hasta seis decimales.
```
```
Ejemplo: En este caso se consideró la clave “MXN” (Peso
Mexicano).
Descuento= 864.10
```
```
Cantidad Valor unitario Importe Descuento
```
```
3.141649 1230.00 3864.22827 864.10
```
```
Los descuentos no se deben registrar de manera global, se
registran por cada uno de los conceptos contenidos dentro del
comprobante.
```
```
Ejemplo:
```
ObjetoImp
Se debe registrar la clave correspondiente para indicar si la
operación comercial es objeto o no de impuesto.

```
 Las claves vigentes se encuentran incluidas en el
catálogo c_ObjetoImp.
```
```
 Si el valor registrado en este campo es “02” (Sí objeto de
impuesto), se deben desglosar los Impuestos a nivel de
Concepto.
```
```
 Si el valor registrado en este campo es “01” (No objeto de
impuesto) o “03” (Sí objeto del impuesto y no obligado al
desglose) no se desglosan impuestos a nivel Concepto.
```
```
Ejemplo:
```
```
ObjetoImp= 02
```
(^)
**c_ObjetoImp Descripción
02** Sí objeto de impuesto


```
Nodo:
Impuestos
```
```
En este nodo se pueden expresar los impuestos aplicables a
cada concepto.
```
```
Si se registra información en este nodo, debe existir al menos
una de las dos secciones siguientes: Traslados o Retenciones.
```
**Nodo:Traslados** En este nodo se pueden expresar los impuestos trasladados
aplicables a cada concepto.

```
Nodo:Traslado En este nodo se debe expresar la información detallada de un
traslado de impuestos aplicable a cada concepto.
```
```
En el caso de que un concepto contenga impuesto trasladado
por Tasa y Cuota, se debe expresar en diferentes apartados.
```
```
Base Se debe registrar el valor para el cálculo del impuesto que se
traslada, puede contener de cero hasta seis decimales.
```
```
El valor de este campo debe ser mayor que cero.
Impuesto Se debe registrar la clave del tipo de impuesto trasladado
aplicable a cada concepto, las cuales se encuentran incluidas en
el catálogo c_Impuesto publicado en el Portal del SAT.
```
```
Ejemplo:
Impuesto= 002
```
```
c_Impuesto Descripción
```
```
001 ISR
```
```
002 IVA
```
```
003 IEPS
```
(^)
(^)
TipoFactor Se debe registrar el tipo de factor que se aplica a la base del
impuesto, el cual se encuentra incluido en el catálogo
c_TipoFactor publicado en el Portal del SAT.
**Ejemplo:**
TipoFactor= **Tasa**


```
c_TipoFactor
Tasa
Cuota
Exento
```
(^)
TasaOCuota Se puede registrar el valor de la tasa o cuota del impuesto que
se traslada para cada concepto. Es requerido cuando el campo
TipoFactor corresponda a Tasa o Cuota.
 Si el valor registrado es fijo debe corresponder a un valor
del catálogo c_TasaOCuota, coincidir con el tipo de
impuesto registrado en el campo Impuesto y el factor
debe corresponder con el campo TipoFactor.
 Si el valor registrado es variable, debe corresponder al
rango entre el valor mínimo y el valor máximo señalado
en el catálogo.
**Ejemplo:**
TasaOCuota= **0.160000
Rango o Fijo
c_TasaOCuota** (^) **Impuest
Valor mínimo Valor máximo o**^ **Factor**^
Fijo No 0.000000 IVA Tasa
Fijo No **0.160000** IVA Tasa
(^)
(^)
(^)
(^)
Importe Se puede registrar el importe del impuesto trasladado que
aplica a cada concepto. No se permiten valores negativos. Este
campo es requerido cuando en el campo TipoFactor se haya
registrado como Tasa o Cuota.
El valor de este campo será calculado por el sistema que genera
el comprobante y considerará los redondeos que tenga
registrado este campo en el estándar técnico del Anexo 20, para
mayor referencia puede consultar la documentación técnica
publicada en el Portal del SAT.
Este campo puede contener de cero hasta seis decimales.
(^)
**Nodo:
Retenciones**
En este nodo se pueden expresar los impuestos retenidos
aplicables a cada concepto.


**Nodo:
Retencion**

```
En este nodo se debe expresar la información detallada de una
retención de impuestos aplicable a cada concepto.
```
```
En el caso de que un concepto contenga impuesto retenido por
Tasa y Cuota, se debe expresar en diferentes apartados.
```
Base Se debe registrar el valor para el cálculo de la retención.

```
Este campo puede tener hasta seis decimales.
```
Impuesto Se debe registrar la clave del tipo de impuesto retenido
aplicable a cada concepto, las cuales se encuentran incluidas en
el catálogo c_Impuesto publicado en el Portal del SAT.

```
Ejemplo:
Impuesto= 001
c_Impuesto Descripción
```
```
001 ISR
```
```
002 IVA
```
```
003 IEPS
```
## TipoFactor Se debe registrar el tipo de factor que se aplica a la base del

```
impuesto, el cual se encuentra incluido en el catálogo
c_TipoFactor en el Portal del SAT y debe ser distinto del valor
“Exento”.
```
```
Ejemplo:
TipoFactor= Tasa
```
```
c_TipoFactor
Tasa
Cuota
Exento
```
(^)
TasaOCuota Se debe registrar el valor de la tasa o cuota del impuesto que se
retiene para cada concepto.
 Si el valor registrado es fijo debe corresponder a un valor
del catálogo c_TasaOCuota, coincidir con el tipo de


```
impuesto registrado en el campo Impuesto y el factor
debe corresponder con el campo TipoFactor.
```
```
 Si el valor registrado es variable, debe corresponder al
rango entre el valor mínimo y valor máximo conforme al
catálogo c_TasaOCuota.
```
```
Ejemplo: En el caso de que la retención del IVA sea de 16%, se
debe registrar de la siguiente forma:
```
```
TasaOCuota= 0.160000
```
```
Ejemplo: En el caso de que la retención del IVA sea de 4%, se
debe registrar de la siguiente forma:
```
```
TasaOCuota= 0.040000
```
(^)
Importe
Se debe registrar el importe del impuesto retenido que aplica a
cada concepto. No se permiten valores negativos.
El valor de este campo será calculado por el sistema que genera
el comprobante y considerará los redondeos que tenga
registrado este campo en el estándar técnico del Anexo 20, para
mayor referencia podrás consultar la documentación técnica
publicada en el Portal del SAT.
Este campo puede contener de cero hasta seis decimales.
**Ejemplo:**
Importe = **8000.00**
Nodo:
ACuentaTercer
os
En este nodo se puede expresar información del contribuyente
tercero, a cuenta del que se realiza la operación. Conforme a la
regla 2.7.1.3 de la Resolución Miscelánea Fiscal vigente.
**Ejemplo:** Cuando el contribuyente “A”, factura a través del
contribuyente “B” derivado de un contrato de comisión o
prestación de servicios de cobranza.
RfcACuentaTer
ceros
Se debe registrar la clave del Registro Federal de
Contribuyentes del contribuyente tercero, a cuenta del que se
realiza la operación.
 La clave registrada en este campo debe ser diferente a la
clave registrada en los campos Rfc del Emisor y Receptor.


```
Ejemplo: En el caso de que el receptor sea una persona física el
“RFC” debe tener una longitud de 13 posiciones, si se trata de
personas morales debe tener una longitud de 12 posiciones.
```
```
Persona física
RfcACuentaTerceros= FIMA420127R44
```
```
Persona moral
RfcACuentaTerceros= COR391215F4A
```
NombreACuent
aTerceros

```
Se debe registrar el nombre, denominación o razón social del
contribuyente tercero correspondiente con el Rfc, a cuenta del
que se realiza la operación.
```
```
El nombre debe corresponder a la clave de RFC registrado en el
campo RfcACuentaTerceros de este Nodo.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
NombreACuentaTerceros= MARTON ALEEJANDRO SANZI
FIERROR
```
```
En el caso de una persona moral se debe registrar:
NombreACuentaTerceros= LA PALMA AEI0
```
RegimenFiscal
ACuentaTercer
os

```
Se debe registrar la clave del régimen del contribuyente tercero,
a cuenta del que se realiza la operación.
```
```
Las claves de los diversos regímenes se encuentran incluidas en
el catálogo c_RegimenFiscal publicado en el Portal del SAT.
```
```
Ejemplo: En el caso de que el tercero sea una persona física
inscrita en el Régimen Arrendamiento, debe registrar lo
siguiente:
```
```
RegimenFiscalACuentaTerceros= 606
```
```
Aplica para tipo persona
```
```
c_RegimenFiscal Descripción Física Moral
```
```
606 Arrendamiento Si No
```

DomicilioFiscal
ACuentaTercer
os

```
Se debe registrar el código postal del domicilio fiscal del tercero,
a cuenta del que se realiza la operación.
```
```
 El código postal debe estar asociado a la clave de RFC
registrado en el campo RfcACuentaTerceros.
```
```
En el caso de las operaciones que se apeguen al Decreto de
estímulos fiscales región fronteriza, se deberá registrar el
código postal del domicilio fiscal o sucursal donde se llevaron a
cabo las operaciones. El código postal deberá ser validado en el
catálogo de código postal a efecto de confirmar que se trata de
una localidad de la zona fronteriza. En este caso no se valida
contra el registrado en la l_RFC.
```
```
Ejemplo:
DomicilioFiscalACuentaTerceros= 01002
```
**Nodo:
InformacionAd
uanera**

```
En este nodo se debe expresar la información aduanera
correspondiente a cada concepto cuando se trate de ventas de
primera mano de mercancías importadas.
```
NumeroPedim
ento

```
Se debe registrar el número del pedimento correspondiente a
la importación del bien, el cual se integra de izquierda a derecha
de la siguiente manera:
```
```
Últimos dos dígitos del año de validación seguidos por dos
espacios, dos dígitos de la aduana de despacho seguidos por
dos espacios, cuatro dígitos del número de la patente seguidos
por dos espacios, un dígito que corresponde al último dígito del
año en curso, salvo que se trate de un pedimento consolidado,
iniciado en el año inmediato anterior o del pedimento original
de una rectificación, seguido de seis dígitos de la numeración
progresiva por aduana.
```
```
 Se debe registrar la información en este campo cuando
el CFDI no contenga el complemento de comercio
exterior (es una venta de primera mano nacional).
```
```
 Para validar la estructura de este campo puede consultar
la documentación técnica publicada en el Portal del SAT.
```
```
Ejemplo:
```

```
NumeroPedimento= 10 47 3807 8003832
```
**Nodo:
CuentaPredial**

```
En este nodo se puede expresar el número de cuenta predial
con el que fue registrado el inmueble en el sistema catastral de
la entidad federativa de que trate, o bien para incorporar los
datos de identificación del certificado de participación
inmobiliaria no amortizable.
```
Numero Se debe registrar el número de la cuenta predial del inmueble
cubierto por cada concepto o bien, para incorporar los datos de
identificación del certificado de participación inmobiliaria no
amortizable, si se trata de arrendamiento.

```
Puede conformarse desde uno hasta 150 dígitos.
```
```
Ejemplo:
Numero= 15956011002
```
**Nodo:
Complemento
Concepto**

```
En este nodo se puede expresar la información adicional
específica de los conceptos registrados en la factura electrónica.
Dichos Complementos Concepto se encuentran publicados en
el Portal del SAT, de acuerdo con las disposiciones particulares
para cada sector o actividad específica.
```
**Nodo: Parte** En este nodo se pueden expresar las partes o componentes que
integran la totalidad del concepto expresado en el
comprobante fiscal digital por Internet.

```
Ejemplo : Venta de 2 KIT de herramientas.
```
```
En este caso para el concepto registrado, cada KIT se integra por
los siguientes artículos: cinco Martillos, cuatro destornilladores,
dos pinzas, de los cuales cada artículo se detalla en una sección
diferente llamada Parte.
```
ClaveProdServ Se debe registrar la clave del producto o del servicio descrito en
la sección llamada Parte.

```
ClaveProdServ NoIdentificacion Cantidad CLaveUnidad Unidad Descripcion ValorUnitario Importe
27113201 hfj68w1 2 KT Kit Conjuntos generales de herramientas$ 2,000.00 $ 4,000.00
```
```
ClaveProdServ NoIdentificacion Cantidad CLaveUnidad Unidad Descripcion ValorUnitarioImporte
Parte 1 41116401 4 10 H87 piezas Martillos de impacto $ 100.00 $ 1,000.00
Parte 2 27111701 56jy 8 H87 piezas Destornillador $ 250.00 $ 2,000.00
Parte 3 27112105 56th8 4 H87 piezas Pinzas $ 250.00 $ 1,000.00
```
```
Concepto
```
```
Partes
```

```
Se deben utilizar las claves de los diversos productos o servicios,
que se encuentran incluidas en el catálogo c_ClaveProdServ
publicado en el Portal del SAT, cuando los conceptos que se
registren por sus actividades correspondan a estos.
```
```
Ejemplo:
ClaveProdServ= 41116401
```
```
c_ClaveProdServ Descripción
```
```
41116401 Martillos de impacto
```
```
01010101 No existe en el
catálogo
```
```
En el caso de que la clave de un producto o servicio no se
encuentre en el catálogo, se debe registrar “01010101”.
```
```
Es importante señalar que la identificación de la clave de
producto o servicio que corresponda conforme al catálogo
c_ClaveProdServ, será responsabilidad del emisor de la factura
en razón de ser él quien conoce las características y la
naturaleza del producto o servicio que comercializa y amparará
el comprobante.
```
```
En el caso de que el emisor del comprobante comercialice
productos que no hayan sido objeto de transformación o
industrialización de su parte –es decir lo compra y tal cual lo
vende-, el emisor podrá utilizar la clave del producto registrada
por su proveedor en el comprobante que ampara la adquisición
de los mismos.
```
NoIdentificacio
n

```
Se puede registrar el número de serie, número de parte del bien
o identificador del producto o del servicio, descrita en la sección
llamada “Parte”.
Opcionalmente se pueden utilizar claves del estándar GTIN
(Número de artículo de comercio global).
```
```
Puede conformarse desde uno hasta 100 caracteres
alfanuméricos.
```
```
Ejemplo: En este caso el número identificador del producto es:
NoIdentificacion= 3nn58
```

```
Ejemplo: En este caso el número identificador del producto
utilizado es un GTIN:
NoIdentificacion= 7501030283645
```
###### 7501030283645

Cantidad Se debe registrar la cantidad de bienes o servicios
correspondiente a la sección llamada Parte.

```
Ejemplo:
Cantidad= 10
```
Unidad Se puede registrar la unidad de medida del bien o servicio
propio de la operación del emisor, aplicable para la cantidad
expresada en la sección llamada Parte.

```
Ejemplo:
Unidad = Piezas
```
Descripcion Se debe registrar la descripción del bien o servicio
correspondiente a la sección llamada Parte.

```
Ejemplo:
Descripcion = Martillos de impacto
```
```
Puede conformarse desde uno hasta 1000 caracteres
alfanuméricos.
```
ValorUnitario Se puede registrar el valor o precio unitario del bien o servicio
correspondiente a la sección llamada Parte, el cual debe ser
mayor que cero.

```
Ejemplo:
ValorUnitario= 100 .00
```
Importe Se puede registrar el importe total de los bienes o servicios de
la presente parte. Debe ser equivalente al resultado de
multiplicar la cantidad por el valor unitario expresado en la


```
parte y considerará los redondeos que tenga registrado este
campo en el estándar técnico del Anexo 20. No se permiten
valores negativos.
```
```
Este campo puede contener de cero hasta seis decimales.
```
```
Ejemplo:
Importe= 1000.00
```
```
Cantidad Valor unitario Importe
```
```
10 100.00 = 1000.00
```
```
Para mayor detalle acerca del cálculo del redondeo puede
consultar la documentación técnica publicada en el Portal del
SAT.
```
**Nodo:
InformacionAd
uanera**

```
En este nodo se debe expresar la información aduanera
correspondiente a cada sección llamada Parte cuando se trate
de ventas de primera mano de mercancías importadas.
```
NumeroPedim
ento

```
Se debe registrar el número del pedimento correspondiente a
la importación del bien, el cual se integra de izquierda a derecha
de la siguiente manera:
```
```
Últimos dos dígitos del año de validación seguidos por dos
espacios, dos dígitos de la aduana de despacho seguidos por
dos espacios, cuatro dígitos del número de la patente seguidos
por dos espacios, un dígito que corresponde al último dígito del
año en curso, salvo que se trate de un pedimento consolidado
iniciado en el año inmediato anterior o del pedimento original
de una rectificación, seguido de seis dígitos de la numeración
progresiva por aduana.
```
```
 Se debe registrar la información en este campo cuando
el CFDI no contenga el complemento de comercio
exterior (es una venta de primera mano nacional).
```
```
 Para validar la estructura de este campo puedes
consultar la documentación técnica publicada en el
Portal del SAT.
```
```
Ejemplo:
NumeroPedimento= 10 47 3807 8003832
```

(^) 
**Nodo:
Impuestos**
En este nodo se debe expresar el resumen de los impuestos
aplicables.
En caso de que el TipoDeComprobante sea “T” (Traslado), o “P”
(Pago), este elemento no debe existir.
TotalImpuestos
Retenidos
Es el total de los impuestos retenidos que se desprenden de los
conceptos contenidos en el comprobante fiscal, el cual debe ser
igual a la suma de los importes registrados en la sección
Retenciones, no se permiten valores negativos y es requerido
cuando en los conceptos se registren impuestos retenidos.
 Este campo debe tener hasta la cantidad de decimales
que soporte la moneda.
**Ejemplo:** En este caso, es una prestación por servicios contables
por $15,000.00, en el que se retiene 10% de ISR y las dos terceras
partes de IVA.
Retención ISR 15000.00 X 10% = 1500.00
Retención IVA 15000.00 X 16% / 3 X 2 = 1600.00
Total **3100.00**
TotalImpuestosRetenidos= **3100.00
Ejemplo:** En este caso es un servicio por comisión por la venta
de productos con alta densidad calórica por $15,000.00, en el
que se retiene 8% de IEPS y las dos terceras partes de IVA.
(^)
Retención IEPS 15000.00 X 8% = 1200.00
Retención IVA * 16200.00 X 16% / 3 X 2 = 1728.00
Total **2928.00
*** La base para calcular la retención del IVA es el importe de la
comisión más el IEPS.
TotalImpuestosRetenidos= **2928.00**


TotalImpuestos
Trasladados

```
Es el total de los impuestos trasladados que se desprenden de
los conceptos contenidos en el comprobante fiscal, el cual debe
ser igual a la suma de los importes registrados en la sección
Traslados, no se permiten valores negativos y es requerido
cuando en los conceptos se registren impuestos trasladados.
```
```
 Este campo debe tener hasta la cantidad de decimales
que soporte la moneda.
```
```
Ejemplo: En este caso es una prestación por servicios
contables por $15,000.00, gravados a la tasa de 16%.
```
```
IVA trasladado 15000.00 X 16% = 2400.00
```
```
Total 2400.00
```
```
TotalImpuestosTrasladados= 2400.00
```
```
Ejemplo: En este caso es un servicio por comisión por la venta
de productos con alta densidad calórica por $15,000.00,
gravado a la tasa de 8% de IEPS y con tasa de 16% de IVA.
```
```
IEPS trasladado 15000.00 X 8% = 1200.00
```
```
IVA trasladado* 16200.00 X 16% = 2592.00
```
```
Total 3792.00
```
```
* La base para calcular el IVA es el importe de la comisión más
el IEPS trasladado.
```
```
TotalImpuestosTrasladados= 3792.00
```
**Nodo:
Retenciones**

```
En este nodo se pueden expresar los impuestos retenidos
aplicables y es requerido cuando en los conceptos se registre
algún impuesto retenido.
```
**Nodo:
Retencion**

```
En este nodo se debe expresar la información detallada de una
retención de un impuesto específico.
```
(^)
Debe haber solo un registro por cada tipo de impuesto retenido.
Impuesto Se debe registrar la clave del tipo de impuesto retenido, mismas
que se encuentran incluidas en el catálogo c_Impuesto
publicado en el Portal del SAT.
**Ejemplo:** Por cada tipo de impuesto se debe registrar la clave
que corresponda, en el caso de servicios contables se tiene 2
tipos de impuesto “001” – ISR y “002” – IVA.


```
Tipo 1
Impuesto Importe
```
```
001 XXXX
```
```
Impuesto= 001
```
```
Tipo 2
Impuesto Importe
```
```
002 XXXX
```
```
Impuesto= 002
```
(^)
Importe Se debe registrar el monto del impuesto retenido, el cual debe
tener hasta la cantidad de decimales que soporte la moneda,
no se permiten valores negativos y debe ser igual al redondeo
de la suma de los importes de los impuestos retenidos
registrados en los conceptos, donde el impuesto sea igual al
campo impuesto de este elemento.
**Ejemplo:** Por cada tipo de impuesto se debe registrar el
importe que corresponda, en el caso de servicios contables se
tiene dos tipos de impuesto “001” – ISR y “002” – IVA.
**Tipo 1**
Impuesto Importe
001 **1500.00**
Importe= **1500.00
Tipo 2**
Impuesto Importe
002 **1600.00**
Importe= **1600.00
Nodo:Traslados** En este nodo se pueden expresar los impuestos trasladados
aplicables, es requerido cuando en los conceptos se registre un
impuesto trasladado.
En el caso de que solo existan conceptos en el CFDI con un
TipoFactor exento, en este nodo solo deben existir los campos
Base, Impuesto y TipoFactor.


**Nodo: Traslado**
En este nodo se debe expresar la información detallada de un
traslado de impuesto específico.
Debe haber solo un registro con la misma combinación de
impuesto, factor y tasa por cada traslado.
Base
Se debe registrar el monto de la base del impuesto trasladado,
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
Ejemplo: Por cada tipo de impuesto se debe registrar el
importe que corresponda, en el caso de servicios contables el
importe de la base que le corresponde es de $15,000.00.
```
```
En caso de que solo existan conceptos con TipoFactor Exento,
la suma de este campo debe ser igual al redondeo de la suma
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
**Ejemplo:** Por cada tipo de impuesto se debe registrar la clave
que corresponda, en el caso de servicios contables se tiene un
solo tipo de impuesto (IVA) trasladado “002”.

```
Impuesto TipoFactor TasaOCuota Importe
```
```
002 XXXX XXXX XXXX
```
```
Impuesto= 002
```

TipoFactor
Se debe registrar el tipo factor que se aplica a la base del
impuesto, mismos que se encuentran incluidos en el catálogo
c_TipoFactor publicado en el Portal del SAT.

```
Ejemplo: Por cada tipo de impuesto se debe registrar el tipo
factor que corresponda, en el caso de servicios contables se
tiene un solo tipo factor de impuesto “Tasa”.
```
```
Impuesto TipoFactor TasaOCuota Importe
```
```
XXXX Tasa XXXX XXXX
```
```
TipoFactor= Tasa
```
TasaOCuota Se puede registrar el valor de la tasa o cuota del impuesto que
se traslada por cada concepto registrado en el comprobante,
mismo que se encuentra incluido en el catálogo c_TasaOCuota
publicado en el Portal del SAT.

```
El valor de la tasa o cuota que se registre debe corresponder a
un registro donde la columna impuesto corresponda con el
campo Impuesto y la columna factor corresponda con el campo
TipoFactor.
```
```
Ejemplo: Por cada tipo de impuesto se debe registrar la tasa o
cuota que corresponda, en el caso de servicios contables se
tiene una sola tasa.
```
```
Impuesto TipoFactor TasaOCuota Importe
```
```
XXXX XXXX 0.160000 XXXX
```
```
TasaOCuota= 0.160000
```
Importe Se puede registrar el monto del impuesto trasladado, agrupado
por Impuesto, TipoFactor y TasaOCuota, el cual debe tener
hasta la cantidad de decimales que soporte la moneda, no se
permiten valores negativos y debe ser igual al redondeo de la
suma de los importes de los impuestos trasladados registrados
en los conceptos, donde el impuesto del concepto sea igual al
campo Impuesto de este apartado y la TasaOCuota del
concepto sea igual al campo TasaOCuota de este apartado.

```
Ejemplo: Por cada tipo de impuesto se debe registrar el
importe que corresponda, en el caso de servicios contables por
```

```
$15,000.00 el importe del impuesto trasladado (IVA) que le
corresponde es de $2,400.00.
```
```
Impuesto TipoFactor TasaOCuota Importe
```
```
XXXX XXXX XXXX 2400.00
```
```
Importe= 2400.00
```
**Nodo:
Complemento**

```
En este nodo se pueden incluir los complementos
determinados por el SAT de acuerdo con las disposiciones
particulares para un sector o actividad específica. Para el caso
del complemento Timbre Fiscal Digital se incluye de manera
obligatoria.
```
```
No permite complementos del comprobante fiscal digital por
Internet que ampara retenciones e información de pagos.
```
**Nodo:
Addenda**

```
En este nodo se pueden expresar las extensiones al presente
formato que sean de utilidad al contribuyente. Para las reglas
de uso del mismo, referirse a la documentación técnica.
```

```
II. Guía de llenado del Comprobante Fiscal Digital por Internet que
```
**_ampara retenciones e información de pagos_**

(^)
Cuando se emita un Comprobante Fiscal Digital por Internet que ampara
retenciones e información de pagos, éste se debe emitir con las especificaciones
señaladas en cada uno de los campos expresados en lenguaje no informático
que se incluyen en esta sección.
En el presente documento se hace referencia a la descripción de la información
que debe contener el citado documento técnico.
Cuando en las siguientes descripciones se establezca el uso de un valor, éste se
señala entre comillas, pero en el Comprobante Fiscal Digital por Internet que
ampara retenciones e información de pagos debe registrarse sin incluir las
comillas y respetar mayúsculas, minúsculas, números, espacios y signos de
puntuación.
**Nombre del
nodo o atributo
Descripción
Nodo: Retenciones**
Estándar del Comprobante Fiscal Digital por
Internet que ampara retenciones e información
de pagos.
Version
Debe tener el valor “2.0”.
Este dato lo integra el sistema que utiliza el
contribuyente para la emisión del comprobante
que ampara retenciones e información de
pagos.
FolioInt
Es el folio de control interno que asigna el
contribuyente emisor al comprobante que
ampara retenciones e información de pagos y
puede conformarse de uno a 20 caracteres
alfanuméricos.

### Sello Es el sello digital del comprobante que ampara

```
retenciones e información de pagos generado
con el certificado de sello digital del
contribuyente emisor del comprobante; éste
funge como la firma del emisor del comprobante
y lo integra el sistema que utiliza el
contribuyente para la emisión del comprobante.
```

NoCertificado Es el número que identifica al certificado de sello
digital del emisor, el cual lo incluye en el
comprobante que ampara retenciones e
información de pagos el sistema que utiliza el
contribuyente para la emisión.

Certificado Es el contenido del certificado del sello digital del
emisor, y lo integra el sistema que utiliza el
contribuyente para la emisión del comprobante
que ampara retenciones e información de
pagos.

FechaExp Se debe registrar la fecha y hora de expedición
del comprobante que ampara retenciones e
información de pagos. Se expresa en la forma
AAAA-MM-DDThh:mm:ss y debe corresponder
con la hora local donde se expide el
comprobante.

```
Ejemplo:
FechaExp= 2017 - 01 - 11T17:28:05
```
LugarExpRetenc Se debe registrar el código postal del lugar de
expedición del comprobante que ampara
retenciones e información de pagos, debe
corresponder con una clave de código postal
vigente incluida en el catálogo de CFDI
c_CodigoPostal.

```
Los distintos códigos postales se encuentran
incluidos en el catálogo de CFDI c_CodigoPostal.
```
```
Ejemplo :
```
```
LugarExpRetenc= 20159
```
```
c_CodigoPostal
20159
```

CveRetenc Se debe registrar la clave vigente de la retención
e información de pagos.

```
Las distintas claves de retención se encuentran
incluidas en el catálogo c_CveRetenc publicado
en el Portal del SAT.
```
```
 Si el valor registrado en este campo es
“25”, se debe registrar información en el
campo DescRetenc.
```
```
 Cuando el catálogo señale un
complemento asociado al tipo de
retención, se debe incluir dicho
complemento en el comprobante.
```
```
Ejemplo:
CveRetenc= 01
```
```
Clave Retenciones
01 Servicios Profesionales
02 Regalías por Derechos de Autor^
```
(^)
DescRetenc
Se debe registrar la descripción por la que se
hace la retención e información de pagos
cuando en el campo CveRetenc se haya
registrado la clave de retención “25” (otro tipo de
retenciones), puede conformarse de 1 a 100
caracteres.
**Ejemplo:** En este caso al tratarse de otro tipo de
retenciones se registró la descripción definida
por el propio emisor.
DescRetenc= **Información referente a la
fiduciaria
Nodo:
CfdiRetenRelacionados**
En este nodo se puede expresar la información
de los comprobantes relacionados
TipoRelacion Se debe registrar la clave vigente de la relación
que existe entre este comprobante que se está
generando y el CFDI que ampara retenciones e
información de pagos previos.


```
La clave de Tipo de relación se encuentra
incluida en el catálogo de CFDI c_TipoRelacion
publicado en el Portal del SAT.
```
```
Ejemplo:
```
```
TipoRelacion= 04
```
```
c_TipoRelacion Descripción
04 Sustitución de los
CFDI previos
```
UUID Se debe registrar el folio fiscal (UUID) de un
comprobante que ampara retenciones e
información de pagos relacionado con el
presente comprobante,

```
Ejemplo:
UUID= 5FB2822E-396D- 4725 - 8521 -
DC4BDD20CCF
```
**Nodo:Emisor**
En este nodo se debe expresar la información del
contribuyente emisor del comprobante que
ampara retenciones e información de pagos.

RfcE Se debe registrar la clave del Registro Federal de
Contribuyentes del emisor del comprobante que
ampara retenciones e información de pagos, sin
guiones o espacios.

```
En el caso de que el emisor sea una persona
física, este campo debe contener una longitud
de 13 posiciones, tratándose de personas
morales debe contener una longitud de 12
posiciones.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
RfcE= CABL840215RF4
```
```
En el caso de una persona moral se debe
registrar:
RfcE= PAL7202161U0
```

NomDenRazSocE
Se debe registrar el nombre, denominación o
razón social del emisor inscrito en el RFC, del
comprobante que ampara retenciones e
información de pagos.

```
El nombre debe corresponder a la clave de RFC
registrado en el campo Rfc de este Nodo.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
NomDenRazSocE = MARTON ALEEJANDRO
SANZI FIERROR
```
```
En el caso de una persona moral se debe
registrar:
NomDenRazSocE= LA PALMA AEI0
```
RegimenFiscalE Se debe registrar la clave vigente del régimen del
contribuyente emisor del comprobante que
ampara retenciones e información de pagos.

```
Las claves de los diversos regímenes se
encuentran incluidas en el catálogo
c_RegimenFiscal, publicado en el Portal del SAT.
```
```
Ejemplo: En el caso de que el emisor sea una
persona moral inscrita en el Régimen General de
Ley de Personas Morales, debe registrar lo
siguiente:
```
```
RegimenFiscalE= 601
```
```
Aplica para tipo persona
```
```
c_RegimenFiscal Descripción Física Moral
```
```
601 General de Ley
Personas
Morales
```
```
No Si
```
```
603 Personas
Morales con
Fines no
Lucrativos
```
```
No Si
```

```
605 Sueldos y
Salarios e
Ingresos
Asimilados a
Salarios
```
```
Si No
```
**Nodo:Receptor** En este nodo se debe expresar la información del
contribuyente receptor del comprobante que
ampara retenciones e información de pagos.

NacionalidadR Se debe registrar la nacionalidad del receptor del
comprobante que ampara retenciones e
información de pagos, el cual acepta
únicamente los valores “Nacional” o “Extranjero”.

**Ejemplo:**
NacionalidadR= **Nacional
Nodo: Nacional** En este nodo se debe expresar la información del
contribuyente receptor del comprobante que
ampara retenciones e información de pagos, en
caso de que sea de nacionalidad mexicana.

RfcR Se debe registrar la clave del Registro Federal de
Contribuyentes del receptor del comprobante
que ampara retenciones e información de
pagos, sin guiones o espacios.

```
 El RFC debe estar contenido en la lista de
RFC (l_RFC) inscritos no cancelados en el
SAT en caso de que sea diferente del RFC
genérico “XAXX010101000”.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
RfcR= CABL840215RF4
```
En el caso de una persona moral se debe
registrar:
RfcR= **PAL7202161U0**
NomDenRazSocR
Se debe registrar el(los) nombre(s), primer
apellido, segundo apellido, según corresponda
denominación o razón social del receptor del
comprobante que ampara retenciones e


```
información de pagos, puede conformarse de 1 a
254 caracteres.
```
```
El nombre debe corresponder a la clave de RFC
registrado en el campo Rfc de este Nodo.
```
```
Ejemplo:
En el caso de una persona física se debe registrar:
NomDenRazSocR = MARTON ALEEJANDRO
SANZI FIERROR
```
```
En el caso de una persona moral se debe
registrar:
NomDenRazSocR= LA PALMA AEI0
```
CurpR Se puede registrar la Clave Única del Registro
Poblacional del receptor del comprobante que
ampara retenciones e información de pagos, se
conforma de 18 caracteres alfanuméricos.

```
En el caso de personas morales, estas no cuentan
con CURP, por tanto, no debe registrar este dato.
```
**Ejemplo** :
CurpR= **VCJE760422MDFRCA03**
DomicilioFiscalR Se debe registrar el código postal del domicilio
fiscal del receptor del comprobante.

```
 El código postal debe estar asociado a la
clave de RFC registrado en el campo Rfc
de este Nodo.
```
```
Ejemplo :
```
```
DomicilioFiscalR= 20150
```
**Nodo:Extranjero** En este nodo se debe expresar la información del
contribuyente receptor del comprobante que
ampara retenciones e información de pagos,
cuando sea residente en el extranjero.

NumRegldTribR Se puede capturar el número de registro de
identificación fiscal del receptor del


```
comprobante que ampara retenciones e
información de pagos, cuando éste sea un
residente en el extranjero, puede conformarse
de 1 a 20 caracteres.
```
```
Ejemplo : En el caso de que el receptor del
comprobante fiscal sea residente en el
extranjero se debe registrar conforme a lo
siguiente:
```
```
NumRegIdTribR= 121585958
```
NomDenRazSocR Se debe registrar el nombre, denominación o
razón social del receptor del comprobante que
ampara retenciones e información de pagos,
cuando se trate de un residente en el extranjero,
puede conformarse de 1 a 300 caracteres.

```
Ejemplo:
En el caso de una persona física se debe registrar:
NomDenRazSocR = VERÓNICAA ERIKKA
HURTTADO LÓPEEZ
```
```
En el caso de una persona moral se debe
registrar:
NomDenRazSocR= LA PALLMMERA
```
**Nodo:Periodo** En este nodo se debe expresar el periodo del
comprobante que ampara retenciones e
información de pagos.

MesIni
Es el mes inicial, el cual se debe registrar la clave
vigente de acuerdo al periodo en que se realizó
la retención o la información de pagos, de
conformidad con la clave contenida en el
catálogo de retenciones c_Periodo.

```
Ejemplo: En el caso de que la retención se haya
realizado el día 18 de enero, se debe registrar lo
siguiente:
```
```
MesIni= 1
```
```
Ejemplo: En el caso de que la retención se haya
efectuado de forma anualizada, se debe registrar
lo siguiente:
```

```
MesIni= 1
```
MesFin Es el mes final, el cual se debe registrar la clave
vigente de acuerdo al periodo en que se realizó
la retención o la información de pagos, de
conformidad con la clave contenida en el
catálogo de retenciones c_Periodo.

```
Ejemplo: En el caso de que la retención se haya
efectuado dentro del mismo periodo (mes de
enero) se debe registrar en este campo el mes
señalado en el campo “MesIni”.
```
```
MesFin= 1
```
```
Ejemplo: En el caso de que la retención se haya
efectuado de forma anualizada, se debe registrar
lo siguiente:
```
```
MesFin= 12
```
Ejercicio Se debe registrar el ejercicio fiscal (año) en el que
se realizó la retención e información del pago.

```
Las distintas claves del ejercicio fiscal se
encuentran incluidas en el catálogo c_Ejercicio.
```
```
El valor registrado debe ser igual al año en curso
o al año inmediato anterior considerando el
registrado en la FechaExp
```
**Ejemplo:**
Ejercicio= **2016
Nodo: Totales** En este nodo se debe expresar el total de las
retenciones e información de pagos efectuados
en el periodo correspondiente al comprobante
que ampara retenciones e información de
pagos.

MontoTotOperacion Se debe registrar el monto total de la operación
que se relaciona en el comprobante que ampara
retenciones e información de pagos. No se
permiten valores negativos.


```
 El valor de este campo debe ser igual a la
suma de los campos MontoTotGrav y
MontoTotExent.
```
```
Ejemplo:
En el caso de que un contribuyente enajene un
bien inmueble por un monto total de $
180,000.00 se debe registrar lo siguiente:
MontoTotOperacion= 180000.00
```
```
Ejemplo:
En el caso de que un contribuyente enajene un
bien inmueble por un monto total de $
190,000.65 se debe registrar lo siguiente:
```
```
MontoTotOperacion= 190000.65
```
MontoTotGrav Se debe registrar el monto total gravado de la
operación que se relaciona en el comprobante
que ampara retenciones e información de
pagos. No se permiten valores negativos.

```
Cuando existan ingresos exentos en este campo
se debe ingresar la diferencia entre el monto
total de la operación menos el ingreso exento.
```
```
 El valor de este campo debe ser menor o
igual al campo MontoTotOperacion.
```
```
Ejemplo: En el caso de que un contribuyente
haya obtenido ingresos por la enajenación de un
bien inmueble por un monto total de $
180,000.00 y no existan ingresos exentos, se
debe registrar lo siguiente:
```
```
MontoTotGrav= 180000.00
```
(^)
**Monto**
Monto total de la operación $ 180000.00
Menos el monto de ingresos
exentos
0
**Monto total del ingreso gravado $ 180000.00**
(^)


MontoTotExent Se debe registrar el monto total exento de la
operación que se relaciona en el comprobante
que ampara retenciones e información de
pagos. No se permiten valores negativos.

```
El valor de este campo debe ser menor o igual al
campo MontoTotOperacion.
```
```
Ejemplo: En el caso de que se enajene un bien
inmueble y no existan ingresos exentos, se debe
registrar en este campo “0”.
```
```
MontoTotExent= 0
```
(^)
**Monto**
Monto total de la operación $ 180000.00
**Menos el monto de ingresos
exentos
0**
Monto total del ingreso gravado $ 180000.00
MontoTotRet
Se debe registrar el total de las retenciones
efectuadas que se relacionan en el comprobante
que ampara retenciones e información de
pagos, es decir, es la suma de los montos de
retención del nodo ImpRetenidos. No se
permiten valores negativos.
Si el valor es mayor que cero, debe existir al
menos un nodo hijo de ImpRetenidos y debe ser
igual a la suma de los campos MontoRet.
**Ejemplo:**
MontoTotRet= **5033.00**
UtilidadBimestral Se puede registrar el monto de la utilidad
bimestral. No se permiten valores negativos.
 Si el valor registrado en el campo
CveRetenc es “28”, el valor de este campo
debe ser mayor a cero y los campos
MontoTotGrav y MontoTotExent deben
tener el valor “0”.
**Ejemplo:**


```
UtilidadBimestral = 1250.00
```
ISRCorrespondiente Se puede registrar el monto del ISR
correspondiente al bimestre. No se permiten
valores negativos.

```
 Si el valor registrado en el campo
CveRetenc es “28”, el valor de este campo
debe ser mayor a cero y los campos
MontoTotGrav y MontoTotExent deben
tener el valor “0”.
```
```
Ejemplo:
ISRCorrespondiente = 750.00
```
**Nodo:ImpRetenidos** En este nodo se puede expresar el total de
impuestos retenidos que corresponden a los
conceptos contenidos en el comprobante que
ampara retenciones e información de pagos.

BaseRet Se puede registrar la base del impuesto, que
puede ser la diferencia entre los ingresos
percibidos y las deducciones autorizadas. No se
permiten valores negativos.

```
Ejemplo:
BaseRet = 8100.00
```
ImpuestoRet Se debe registrar la clave vigente del tipo de
impuesto retenido en el periodo o ejercicio que
se registra de acuerdo con el catálogo de CFDI
c_Impuesto, el cual se encuentra publicado en el
Portal del SAT.

```
Ejemplo: En el caso de que se haya enajenado
un bien inmueble y resulte ISR a retener se debe
registrar lo siguiente:
```
```
ImpuestoRet= 01
```
```
Clave Tipo de Impuesto
01 ISR
02 IVA
```
(^)


MontoRet
Se debe registrar el importe del impuesto
retenido de la operación ya sea en el periodo o
en el ejercicio que se relaciona en el
comprobante que ampara retenciones e
información de pagos. No se permiten valores
negativos.

```
Ejemplo:
MontoRet= 5033.00
```
TipoPagoRet Se debe registrar la clave vigente del tipo del
efecto que se le da al monto de la retención, de
acuerdo con el catálogo c_TipoPagoRet, el cual
se encuentra publicado en el portal del SAT,
donde la columna Tipo impuesto debe
corresponder con el tipo de impuesto registrado
en el campo ImpuestoRet.

```
Ejemplo:
TipoPagoRet= “01” (Pago definitivo IVA)
```
**Nodo: Complemento**
En este nodo se pueden incluir los
complementos determinados por el SAT de
acuerdo con las disposiciones particulares para
un sector o actividad específica. Para el caso del
complemento Timbre Fiscal Digital se incluye de
manera obligatoria.

**Nodo: Addenda**
En este nodo se pueden expresar las extensiones
al formato que sean de utilidad al contribuyente.
Para las reglas de uso del mismo, referirse a la
documentación técnica.


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

**Nota 2:** Los impuestos locales se deben registrar en el “Complemento Impuestos
Locales”, mismo que podrá consultar en el Portal del SAT en Internet, en la
siguiente dirección electrónica:

https://www.sat.gob.mx/consulta/18002/genera-tus-facturas-electronicas-con-
la-informacion-de-otros-derechos-e-impuestos

**Nota 3:** El Anexo 20 es un estándar técnico predefinido y cerrado por lo que, en
los CFDI que se expidan derivados de la celebración de contratos de obras
públicas y/o inmuebles, las penalizaciones o incumplimientos a dichos contratos,
se podrán incluir en la Addenda.

**Nota 4:** La representación impresa del CFDI deberá cumplir al menos con los

datos mínimos que establecen las reglas 2.7.1.7., y en el caso de nómina 2.7.5.2., de

la Resolución Miscelánea Fiscal vigente.


```
Apéndice 2 Clasificación de los tipos de CFDI
```
**Tipos de comprobantes:**

1. **Comprobante de Ingreso.** - Se emiten por los ingresos que obtienen los

```
contribuyentes, ejemplo: prestación de servicios, arrendamiento, honorarios,
donativos recibidos, enajenación de bienes y mercancías, incluyendo la
enajenación que se realiza en operaciones de comercio exterior, etc.
```
#### 2. Comprobante de Egreso. - Amparan devoluciones, descuentos y

```
bonificaciones para efectos de deducibilidad y también puede utilizarse para
corregir o restar un comprobante de ingresos en cuanto a los montos que
documenta, como la aplicación de anticipos. Este comprobante es conocido
como nota de crédito.
```
#### 3. Comprobante de Traslado. - Sirve para amparar el transporte, la legal

```
tenencia y estancia de las mercancías objeto del transporte durante su
trayecto en territorio nacional. También puede usarse para documentar
operaciones de transporte de mercancías al extranjero.
```
#### 4. Comprobante de Recepción de pagos. – Es un CFDI que incorpora un

```
Complemento para recepción de Pagos, el cual debe emitirse en los casos de
operaciones con pago en parcialidades o cuando al momento de expedir el
CFDI no se reciba el pago de la contraprestación y facilita la conciliación de las
facturas contra pagos.
```
#### 5. Comprobante de Nómina. - Es un CFDI al que se incorpora el complemento

```
recibo de pago de nómina, el cual debe emitirse por los pagos realizados por
concepto de remuneraciones de sueldos, salarios y asimilados a estos, es una
especie de una factura de egresos.
```
6. **Comprobante de Retenciones e información de pagos**. - Se expiden en las

```
operaciones en las cuales se informa de la realización de retenciones de
impuestos, incluyendo el caso de pagos realizados a residentes en el extranjero
para efectos fiscales y las retenciones que se les realicen; este tipo de
comprobante no forma parte del Catálogo tipo de comprobante porque éste
se genera con el estándar contenido en el rubro II. del Anexo 20.
```

```
Apéndice 3 Clasificación de Productos y Servicios
```
A continuación, se explica cómo realizar la búsqueda de un producto o servicio
en el Catálogo del Anexo 20.

Esta es la forma en la que se integra una clave de producto o servicio:

```
Nivel Ubicación
```
```
1 .División Los dos primeros dígitos
```
```
2.Grupo Los siguientes dos dígitos
```
```
3.Clase Los siguientes dos dígitos
```
```
4.Producto Los últimos dos dígitos
```
1.- Inicia por ubicar la descripción de tu producto o servicio utilizando la División

conforme a lo siguiente, ubica el producto o servicio conforme a la descripción y
a continuación identifica los dos primeros dígitos que corresponden a la División:

```
Descripción de la División Dígitos
```
```
Materias primas, químicos, papel y
combustibles
```
```
10000000 – Materiales relacionados
con la fauna, semillas y flora
11000000 - Materiales de Minerales y
Tejidos y de Plantas y Animales no
Comestibles
12000000 - Productos químicos
incluyendo los bio-químicos y gases
industriales
13000000 - Resina y Colofonia y
Caucho y Espuma y Película y
Materiales Elastoméricos
14000000 - Materiales y Productos de
Papel
15000000 - Combustibles
```
Herramientas y equipos industriales (^20000000) - Maquinaria de minería y
perforación de pozos y accesorios
21000000 - Maquinaria y Accesorios
para Agricultura


```
Descripción de la División Dígitos
```
```
23000000 - Maquinaria y Accesorios
de Fabricación y Transformación
Industrial
24000000 - Maquinaria y Accesorios
de Embalaje y Contenedores
26000000 - Maquinaria y Accesorios
para Generación y Distribución de
Energía
27000000 - Herramientas y
Maquinaria en General (equipo
hidráulico y neumático)
```
Suministros y componentes (^30000000) - Componentes y
Suministros de Fabricación y
Construcción
31000000 - Componentes y
Suministros de Fabricación
32000000 - Componentes y
Suministros Electrónicos
39000000 - Suministros de
Iluminación y Electrónica
Suministros y equipos de
construcción, edificaciones y
transportes
22000000 - Maquinaria y Accesorios
para Construcción y Edificación
25000000 – Vehículos y Medios de
Transportación
40000000 - Sistemas de calefacción,
Tubería y Ventilación
Productos farmacéuticos, y
suministros y equipos de ensayo, de
laboratorio y médicos
41000000 - Equipo de Laboratorio
42000000 - Equipo Veterinario,
Médicos, y Ortopédico
51000000 - Medicamentos y
Productos Farmacéuticos
Suministros y equipos de servicios,
limpieza y comida
47000000 - Equipo y Suministros de
limpieza


```
Descripción de la División Dígitos
```
48000000 – Maquinaria y Equipos de

```
cocina
50000000 - Alimentos
```
Suministros y equipos tecnológicos,

de comunicaciones y de negocios

```
43000000 - Telecomunicaciones y
radiodifusión de tecnología de la
información
44000000 - Equipo
45000000 - Equipo y Suministros de
Imprenta
55000000 - Productos Impresos
```
Suministros y equipos de defensa y

seguridad

```
46000000 - Equipos y Suministros de
Defensa
```
Suministros y equipos de consumo,

domésticos y personales

```
49000000 – Equipos de deporte,
accesorios y recreativos
52000000 – Muebles, Utensilios de
cocina, Electrodomésticos y
Accesorios para el hogar
53000000 – Ropa, calzado, maletas y
artículos de tocador
54000000 - Productos para Relojería y
Bisutería
56000000 - Muebles y mobiliario
60000000 – Productos de papelería
escolares, musicales y juguetes
```
Servicios
64000000 – Contratos de seguro de
salud
70000000 - Servicios relacionados del
sector primario
71000000 - Servicios de Perforación de
Minería
72000000 - Servicios de Construcción
y Mantenimiento


```
Descripción de la División Dígitos
```
```
73000000 - Servicios de Producción y
Fabricación Industrial
76000000 - Servicios de Limpieza
Industrial
77000000 - Servicios relacionados con
el medio ambiente
78000000 - Servicios de Transporte
80000000 - Servicios de Gestión y
Administrativos
81000000 - Servicios basados en
ingeniería
82000000 - Servicios Editoriales y
Publicidad
83000000 - Servicios Públicos y
Servicios Relacionados con el Sector
Público
84000000 - Servicios Financieros y de
Seguros
85000000 - Servicios Sanitarios y
Hospitalarios
86000000 - Servicios Educativos y de
Formación
90000000 - Servicios de Viajes y
Alimentación
91000000 - Servicios Personales y
Domésticos
92000000 - Servicios de Defensa
Nacional
93000000 - Servicios Políticos y de
Asuntos Cívicos
94000000 - Organizaciones y Clubes
```
Bienes Inmuebles 95000000.-Bienes inmuebles


2. Una vez que ya cuentas con los dos primeros dígitos de la División, puedes

también verificar entre las descripciones de esta División cuál es la que describe

tu producto o servicio, aquí pueden darse dos situaciones, a saber:

```
a) Que encuentres la descripción precisa de tu producto o servicio, o;
b) Que no encuentres una descripción de tu producto o servicio.
```
En el caso b), puedes seleccionar la clave que, sin describir de manera precisa o

exacta tu producto o servicio, sea la que a tu consideración se acerque más a

ella.

Este es un ejemplo de clasificación en un supuesto como el del caso b):

Ejemplo: Ubicación en el Catálogo de productos y servicios del Anexo 20 del

producto “Donas glaseadas”.

Para efectos del registro del campo “ClaveProdServ” del Anexo 20, basta con que

el contribuyente clasifique la descripción del bien o servicio hasta el tercer nivel,

es decir hasta la clase.

Para facilitar la clasificación de bienes o servicios y unidad de medida el SAT pone

a disposición de los contribuyentes una herramienta de búsqueda de las mismas,
esta herramienta está disponible en el Portal del SAT en Internet, en la sección

de factura.


Dando clic en el botón de la siguiente dirección electrónica:
https://www.sat.gob.mx/consultas/53693/catalogo-de-productos-y-servicios

Es importante no perder de vista que la inclusión en el comprobante de estas

claves de producto o servicio y de unidad, son datos que no sustituyen a la
descripción del producto o servicio que registra cada contribuyente en sus
comprobantes ni a la clave de producto o servicio interna que cada

contribuyente maneja, por lo que solo las complementan.

Solo en el caso extremo de que no se pudiera identificar algún producto o servicio

dentro del catálogo, ni siquiera buscando alguna clasificación que se acerque o
asemeje, se podrá utilizar la clave 01010101 “No existe en el catálogo”.


```
Apéndice 4 Catálogos del comprobante
```
Los catálogos contienen el detalle de las claves y descripciones que facilitan el

llenado del comprobante y se pueden consultar dando clic en el botón
de la siguiente dirección electrónica:

https://www.sat.gob.mx/consultas/35025/formato-de-factura-electronica-
(anexo-20)


```
Apéndice 5 Emisión de CFDI de Egresos
```
Los nodos y campos no mencionados en este procedimiento, se deben registrar
en el comprobante fiscal conforme a las especificaciones contenidas en el punto
I. de esta Guía.

**I. Emisión de CFDI de tipo “E” (Egreso) relacionado a varios comprobantes:**

**Ejemplo:** Se tienen tres comprobantes de tipo “I” (Ingreso) con la siguiente
información.

CFDI 1: Con un importe de $100.00 y forma de pago “01” Efectivo.
CFDI 2: Con un importe de $150.00 y forma de pago “02” Cheque nominativo.
CFDI 3: Con un importe de $200.00 y forma de pago “03” Transferencia
electrónica de fondos.

Se requiere realizar un descuento, devolución o bonificación de operaciones
documentadas en los CFDI anteriores por el 100% del valor de los tres
comprobantes.

En este supuesto, el CFDI de tipo “E” (egreso) se emite conforme a lo siguiente:

```
 Registrar como importe el total de la sumatoria de los comprobantes de
tipo “I” (Ingreso) en este ejemplo $450.00
 Registrar en el Nodo: CfdiRelacionado, cada uno de los CFDI de tipo “I”
(Ingreso) (un nodo por cada UUID de los comprobantes 1,2 y 3).
 Registrar en forma de pago, conforme a las siguientes opciones:
```
```
a) Se registra la forma de pago con la que se está efectuando el
descuento, devolución o bonificación en su caso.
b) Si el o los CFDI de tipo “I” (Ingreso) no han sido aún pagados, puede
registrarse como forma de pago la clave “15” (Condonación).
```
```
Nota: Es muy importante considerar que el uso de la forma de pago con clave
“15” (Condonación) que se establece en el inciso b) que antecede, es una
definición de forma y que ésta se propone ante el hecho de la inexistencia de
un pago y la necesidad de tener que llenar este campo para poder emitir el
CFDI.
```
```
 Registrar en método de pago la clave “PUE” (Pago en una sola exhibición).
```
```
 Registrar en el campo ClaveProdServ, la clave que corresponda según el
caso o la clave “84111506” (Servicios de facturación).
```
```
 Registrar en el campo ClaveUnidad, la clave que corresponda según el
caso, o la clave “ACT” (Actividad).
```

**II. Emisión de CFDI de tipo “E” (Egreso) relacionado a un comprobante:**

En caso de que existan varios comprobantes de tipo “I” (Ingreso) en los cuales se
requiera aplicar descuento, devolución o bonificación con un valor menor al
importe de cada uno de los comprobantes, se podrá emitir un CFDI de tipo “E”
(Egreso) por cada descuento, devolución o bonificación que aplique a cada
comprobante de tipo “I” (Ingreso), registrando la forma de pago con la que se está
efectuando el descuento, devolución o bonificación.

**Ejemplo:** Se tiene un comprobante de tipo “I” (Ingreso) con la siguiente
información.

CFDI: Con un importe de $200.00 y forma de pago “03” Transferencia electrónica
de fondos.

Se requiere realizar un descuento, devolución o bonificación de la operación
documentada en el CFDI anterior por un valor menor al importe registrado en el
referido comprobante, en este caso $50.00.

En este supuesto, el CFDI de tipo “E” (Egreso) se emite conforme a lo siguiente:

```
 Registrar como importe en este caso $50.00.
```
```
 Registrar en el Nodo: CfdiRelacionado, el CFDI de tipo “I” (Ingreso).
```
```
 Registrar en forma de pago:
```
```
a) La forma de pago con la que se está efectuando el descuento,
devolución o bonificación, en su caso.
b) Si el CFDI de tipo “I” (Ingreso) no ha sido aún pagado, se podrá
registrar como forma de pago la clave “15” (Condonación).
```
```
Nota: Es muy importante considerar que el uso de la forma de pago con clave
“15” (Condonación) que se establece en el inciso b) que antecede, es una
definición de forma y que ésta se propone ante el hecho de la inexistencia de
un pago y la necesidad de tener que llenar este campo para poder emitir el
CFDI.
```
```
 Registrar en método de pago la clave “PUE” (Pago en una sola exhibición).
```
```
 Registrar en el campo ClaveProdServ, la clave que corresponda según el
caso o la clave “84111506” (Servicios de facturación).
```
```
 Registrar en el campo ClaveUnidad, la clave que corresponda según el
caso o la clave “ACT” (Actividad).
```

**III. Emisión de un CFDI de tipo “E” (Egreso) relacionado a varios
comprobantes con un importe menor al CFDI de tipo “I” (Ingreso).**

En caso de que existan varios comprobantes de tipo “I” (Ingreso) en los cuales se
requiera aplicar descuento, devolución o bonificación con un valor menor al
importe de cada uno de los comprobantes, se podrá emitir un CFDI de tipo “E”
(Egreso) por el total de los descuentos, devoluciones o bonificaciones que
apliquen de cada comprobante de tipo “I” (Ingreso).

**Ejemplo:** Se tienen dos comprobantes de tipo “I” (Ingreso) con la siguiente
información.

CFDI 1: Con un importe de $150.00 y forma de pago “02” Cheque nominativo.
CFDI 2: Con un importe de $200.00 y forma de pago “03” Transferencia
electrónica de fondos.

Se requiere realizar un descuento, devolución o bonificación de operaciones
documentadas en los CFDI anteriores por 10% del valor de los dos comprobantes.

En este supuesto, el CFDI de tipo “E” (Egreso) se emite conforme a lo siguiente:

```
 Registrar como importe el total de la sumatoria del descuento a los
comprobantes de tipo “I” (Ingreso), en este ejemplo $35.00.
 Registrar en el Nodo: CfdiRelacionado, cada uno de los CFDI de tipo “I”
(Ingreso) (un nodo por cada UUID de los comprobantes 1 y 2).
 Registrar en forma de pago, conforme a las siguientes opciones:
```
```
a) Se registra la forma de pago con la que se está efectuando el
descuento, devolución o bonificación en su caso.
b) Si el o los CFDI de tipo “I” (Ingreso) no han sido aún pagados, se
podrá registrar como forma de pago la clave “15” (Condonación).
```
```
Nota: Es muy importante considerar que el uso de la forma de pago con clave
“15” (Condonación) que se establece en el inciso b) que antecede, es una
definición de forma y que ésta se propone ante el hecho de la inexistencia de
un pago y la necesidad de tener que llenar este campo para poder emitir el
CFDI.
```
```
 Registrar en método de pago la clave “PUE” (Pago en una sola exhibición).
```
```
 Registrar en el campo ClaveProdServ, la clave que corresponda según el
caso o la clave “84111506” (Servicios de facturación).
```
```
 Registrar en el campo ClaveUnidad, la clave que corresponda según el
caso o la clave “ACT” (Actividad).
```

```
 Registrar en el campo Descripcion el monto del descuento, devolución o
bonificación que le aplique a cada comprobante de ingresos e indicar a
qué comprobante de ingresos aplica el mismo, por ejemplo “3% del saldo
de todos los CFDI relacionados”, o “3% del saldo de los CFDI con folios... y
5% del saldo de los CFDI con folios...”
```
**IV. Emisión de un CFDI de tipo “E” (Egreso) relacionado a un CFDI de tipo “I”
(Ingreso) a futuro.**

**Descuentos globales.**

En el caso de generación y aplicación de descuentos globales que hagan los
contribuyentes a ventas futuras, podrán emitir el CFDI de egresos que ampare el
concepto de descuento conforme a cualquiera de las siguientes opciones:

```
A. CFDI de Egresos relacionado a un CFDI de Ingresos.
Cuando se devengue o genere el derecho de un descuento global en un
futuro, el contribuyente podrá, para efectos de control, emitir un
documento interno que demuestre contablemente dicho descuento, para
aplicarlo una vez que se genere el ingreso en el futuro.
```
```
Una vez que se dé el ingreso futuro al cual se aplicará el descuento
previsto, se deberá primero expedir el CFDI de tipo “I” (Ingreso)
correspondiente y a continuación emitir el CFDI de tipo “E” (Egreso) que
ampare el valor consignado en el documento interno de control,
debiéndolo relacionar el CFDI de “E” (Egreso) con el CFDI de “I” (Ingreso).
```
```
B. CFDI de Egresos emitido sin relacionar a un CFDI de Ingresos.
Cuando se devengue o genere el derecho de un descuento global en un
futuro, el contribuyente podrá emitir un CFDI de tipo “E” (Egreso) por el
valor del descuento sin relacionarlo a un CFDI de “I” (Ingreso), registrando
en el campo FormaPago la clave “23” (Novación).
```
```
Nota: Es muy importante considerar que el uso de la forma de pago con clave “23”
(Novación) que se establece en el párrafo que antecede, es solo una definición de forma
y que tiene el objeto de identificar a los CFDI que aplican descuentos a futuro.
```
```
Una vez que se genere el ingreso en el futuro, se debe emitir el CFDI de
tipo “I” (Ingreso) correspondiente al cual se le debe relacionar el CFDI de
“E” (Egreso), señalado en el párrafo anterior, debiendo registrar en el
campo TipoRelacion la clave “02” (Nota de débito de los documentos
relacionados), y como forma de pago la clave “23” (Novación), siempre y
cuando sea por el mismo monto del CFDI de tipo “E” (Egreso).
```
```
Cuando el monto del CFDI de ingresos que se va a emitir para relacionar la
nota de crédito descrita en el primer párrafo de esta opción sea mayor, se
```

deben emitir dos CFDI, uno por el mismo valor del CFDI de egresos, el cual
se va a emitir con las características indicadas en el párrafo anterior y el
otro por la diferencia, en el cual se va a registrar la forma de pago con la
que se haya liquidado la operación, o bien, si se pactó en parcialidades o
diferido se debe registrar la clave “ 99 ” (Por definir) y cuando se reciba el o
los pagos de éste se debe emitir el CFDI con Complemento para recepción
de Pagos (Ver “Guía de llenado del comprobante al que se le incorpore el
complemento para recepción de pagos”).


**_Apéndice 6 Procedimiento para la emisión de los CFDI en el caso de_**

**_anticipos recibidos_**

Los nodos y campos no mencionados en este procedimiento, se deben registrar
en el comprobante fiscal conforme a las especificaciones contenidas en el punto
I. de esta Guía.

Consideraciones previas.

Este procedimiento es solo para la facturación de operaciones en las cuales

existen pagos de anticipos, por lo que es importante tener en cuenta lo siguiente:

```
I. Si la operación de que se trata se refiere a la entrega de una cantidad
por concepto de garantía o depósito, es decir, la entrega de una
cantidad que garantiza la realización o cumplimiento de alguna
condición, como sucede en el caso del depósito que en ocasiones se
realiza por el arrendatario al arrendador para garantizar el pago de las
rentas en el caso de un contrato de arrendamiento inmobiliario, no
estamos ante el caso de un anticipo.
```
```
II. En el caso de operaciones en las cuales ya exista acuerdo sobre el bien
o servicio que se va a adquirir y de su precio, aunque se trate de un
acuerdo no escrito, y el comprador o adquirente del servicio realiza el
pago de una parte del precio, estamos ante una venta en parcialidades
```
y no ante un anticipo.^

Solo estaremos ante el caso de una operación en dónde existe el pago de un

anticipo, cuando se realice un pago en una operación en dónde:

```
a. No se conoce o no se ha determinado el bien o servicio que se va a
adquirir o el precio del mismo.
```
```
b. No se conoce o no se ha determinado ni el bien o servicio que se va
a adquirir ni el precio del mismo.
```
**Nota:** En el caso de operaciones mensuales con clientes (frecuentes), cuando

estos liquiden la factura en la que en el monto del pago monetario existan

diferencias de centavos y hasta un peso, podrás conservar dichas diferencias en

una cuenta de orden y aplicarla como pago a las facturas siguientes al mismo

cliente, siempre y cuando, esta aplicación se realice dentro de los dos meses

calendario inmediatos siguientes a la realización del pago en dónde existan las

citadas diferencias, en caso contrario, será obligatorio emitir un CFDI por

anticipos para este tipo de operaciones.


**A. Facturación aplicando anticipo con CFDI de egreso.**

```
I. Emisión de un CFDI por el valor del anticipo recibido:
```
El contribuyente al momento de recibir un anticipo debe emitir un comprobante

fiscal digital por Internet (CFDI) por el valor del anticipo y deberá registrar en los

siguientes campos la información que a continuación se describe:

```
a) TipoDeComprobante: En este campo se debe registrar la
clave “I” (Ingreso) del catálogo c_TipoDeComprobante.
```
```
b) FormaPago: En este campo se debe registrar la clave
del catálogo c_FormaPago conforme a lo siguiente:
```
```
a. Si es un anticipo, se debe registrar la clave con
la que se realizó el pago.
```
```
b. Si es un anticipo usando el saldo remanente de
un pago previo se debe registrar la clave con la
que se realizó el pago.
```
```
c) MetodoPago: En este campo se debe registrar la
clave “PUE” (Pago en una sola exhibición) del
catálogo c_MetodoPago
```
```
d) Nodo: CfdiRelacionados: Este nodo no debe existir.
```
```
e) Nodo: Concepto: Solo debe existir un concepto en
este comprobante.
```
```
ClaveProdServ: En este campo se debe registrar la
clave “84111506” (Servicios de facturación).
```
```
Cantidad: Se debe registrar el valor “1”.
```
```
ClaveUnidad: Se debe registrar la clave “ACT”
(Actividad).
```
```
Descripcion: En este campo se debe registrar el valor
“Anticipo del bien o servicio”.
```
```
ValorUnitario: En este campo se debe registrar el
monto entregado como anticipo antes de impuestos.
```

```
II. Emisión de un CFDI por el valor total de la operación.
```
El contribuyente al momento de concretar la operación y recibir el pago de la

contraprestación, debe emitir un CFDI de tipo “I” (Ingreso) y registrar en los

siguientes campos la información que a continuación se describe:

```
a) FormaPago: En este campo se debe registrar la clave de forma de
pago que corresponda de acuerdo al catálogo c_FormaPago.
```
##### b) MetodoPago: En este campo se debe registrar la clave del método

##### de pago que corresponda al catálogo c_MetodoPago.^

```
c) Nodo: CfdiRelacionados: Este nodo debe existir.
 TipoRelacion: En este campo se debe registrar la clave
“07” (CFDI por aplicación de anticipo) del catálogo
c_TipoRelacion, a efecto de relacionar este comprobante
con el del anticipo emitido anteriormente.
```
```
 Nodo: CfdiRelacionado: Este nodo debe existir.
```
```
o UUID: En este campo se debe registrar el o los
folios fiscales del comprobante (anticipo) a 36
```
posiciones que se relacionan a esta factura.^

Es importante mencionar que si en el momento de emitir el CFDI por el valor

total de la operación, no se realiza el pago de la diferencia que resulte entre el

CFDI por el valor total de la operación y el CFDI de “Egreso”, se debe emitir un

CFDI con “Complemento para recepción de pagos” por cada pago recibido.

**III. Emisión de un CFDI de tipo “Egreso”.**

Posteriormente a la emisión del CFDI por el valor total de la operación, el

contribuyente debe emitir un CFDI de tipo “Egreso” por el valor del anticipo

aplicado y registrar en los siguientes campos la información que a continuación

se describe:

```
a) TipoDeComprobante: En este campo se debe registrar
la clave “E” (Egreso) del catálogo
c_TipoDeComprobante.
```
```
b) FormaPago: En este campo se debe registrar la clave
“30” (Aplicación de anticipo) del catálogo c_FormaPago.
```

```
c) MetodoPago: En este campo se debe registrar la clave
“PUE” (Pago en una sola exhibición) del catálogo
c_MetodoPago.
d) Nodo: CfdiRelacionados: Este nodo debe existir.
```
```
 TipoRelacion: En este campo se debe registrar la clave
“07” (CFDI por aplicación de anticipo) del catálogo
c_TipoRelacion, a efecto de relacionar este comprobante
con el CFDI por el valor total de la operación emitido
anteriormente.
```
```
 Nodo CfdiRelacionado: Este nodo debe existir.
```
```
o UUID: Se debe registrar el folio fiscal del
comprobante emitido por el valor total de la
operación a 36 posiciones que se relaciona a esta
factura.
```
```
e) Nodo: Concepto: Solo debe existir un concepto en este
comprobante.
```
```
 ClaveProdServ: En este campo se debe registrar la clave
“84111506” (Servicios de facturación).
```
```
 Cantidad: Se debe registrar el valor “1”.
```
```
 ClaveUnidad: Se debe registrar la clave “ACT” (Actividad).
```
```
 Descripcion: En este campo se debe registrar el valor
“Aplicación de anticipo”.
```
```
 ValorUnitario: En este campo se debe registrar el monto
descontado como anticipo antes de impuestos.
```
Se precisa que la fecha de emisión del CFDI de tipo “I” (Ingreso) por el valor total

de la operación y el CFDI de tipo “E” (Egreso) debe ser preferentemente la misma,

debiendo emitir primero el CFDI de tipo “I” (Ingreso) por el valor total de la

operación y posteriormente el CFDI de tipo “E” (Egreso).


**Ejemplo:**

```
B. Facturación aplicando anticipo con remanente de la contraprestación
```
**I.**^ **Emisión de un CFDI por el valor del anticipo recibido:**^

El contribuyente al momento de recibir un anticipo debe emitir comprobante

fiscal digital por Internet (CFDI) por el valor del anticipo y deberá registrar en los

siguientes campos la información que a continuación se describe:

```
a) TipoDeComprobante: En este campo se debe registrar la
clave “I” (Ingreso) del catálogo c_TipoDeComprobante.
```
```
b) FormaPago: En este campo se debe registrar la clave
con la que se realizó el pago, del catálogo
c_FormaPago.
```
```
c) MetodoPago: En este campo se debe registrar la
clave “PUE” (Pago en una sola exhibición) del
catálogo c_MetodoPago.
```
```
d) Nodo: CfdiRelacionados: Este nodo no debe existir.
```
```
e) Nodo: Concepto: Solo debe existir un concepto en
este comprobante.
```
```
 ClaveProdServ: En este campo se debe
registrar la clave “84111506” (Servicios de
facturación).
```

```
 Cantidad: Se debe registrar el valor “1”.
```
```
 ClaveUnidad: Se debe registrar la clave “ACT”
(Actividad).
```
```
 Descripcion: En este campo se debe registrar
el valor “Anticipo del bien o servicio”.
```
```
 ValorUnitario: En este campo se debe registrar
el monto entregado como anticipo antes de
impuestos.
```
```
II. Emisión de un CFDI por el remanente de la contraprestación,
relacionando el anticipo recibido.
```
El contribuyente al recibir el pago del remanente de la contraprestación, debe

emitir un CFDI por el monto del remanente y registrar en los siguientes campos

la información que a continuación se describe:

```
TipoDeComprobante: Se debe registrar la clave “I” (Ingreso) del
catálogo c_TipoDeComprobante.
```
```
FormaPago: Se debe ingresar la clave del catálogo c_FormaPago
con la que se realizó el pago.
```
```
MetodoPago: Se debe registrar la clave del catálogo
c_MetodoPago que le corresponda.
```
```
Nodo: CfdiRelacionados: Debe de existir.
```
```
TipoRelacion: Se debe registrar la clave “07” (CFDI por aplicación
de anticipo) del catálogo c_TipoRelacion, a efecto de relacionar
este comprobante con el del anticipo emitido anteriormente.
```
```
UUID del nodo CfdiRelacionado: Se debe registrar las 36
posiciones del folio fiscal del comprobante que ampara el anticipo.
```
```
Descripcion del nodo Concepto: En este campo se debe registrar
la descripción del bien o servicio propia de la empresa por cada
concepto, seguido de la leyenda; CFDI por remanente de un
anticipo.
```
```
ValorUnitario: Se deberá registrar por cada concepto el valor del
bien o del servicio.
```

```
Descuento: Se debe registrar por cada concepto el monto del
anticipo.
```
**Ejemplo:**


```
Apéndice 7 Preguntas y respuestas sobre el Anexo 20 versión 4.0
```
**1. ¿Se deberá cancelar el CFDI cuando el receptor dará un uso diferente al**
    **señalado en el campo UsoCFDI?**

```
Sí, se debe cancelar y sustituir por el CFDI que contenga la clave del
UsoCFDI correcta.
```
```
Fundamento Legal: Artículo 29-A, primer párrafo, fracción IV del CFF, regla 2.7.1.29.
de la Resolución Miscelánea Fiscal vigente y Anexo 20 “Guía de llenado de los
comprobantes fiscales digitales por Internet” versión 4.0, publicada en el Portal
del SAT.
```
**2. ¿En el CFDI versión 4.0 se podrán registrar cantidades en negativo?**

```
No, en la versión 4.0 del CFDI no aplica el uso de números negativos para
ningún dato.
```
```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
**3. ¿Cómo se deben reflejar los impuestos retenidos y trasladados en el**
    **CFDI versión 4.0?**

```
En la versión 4.0 del CFDI se expresarán los impuestos trasladados y
retenidos aplicables por cada concepto registrado en el comprobante,
debiéndose detallar lo siguiente:
```
```
 Base para el cálculo del impuesto.
 Impuesto (Tipo de impuesto ISR, IVA, IEPS).
 Tipo factor (Tasa, cuota o exento).
 Tasa o cuota (Valor de la tasa o cuota que corresponda al impuesto).
 Importe (Monto del impuesto).
```
```
Se debe incluir a nivel comprobante el resumen de los impuestos
trasladados por Tipo de impuesto, Tipo factor, Tasa o cuota e Importe.
```
```
Se debe incluir a nivel comprobante el resumen de los impuestos retenidos
por Impuesto e Importe.
```
```
Asimismo, se debe registrar en su caso, el Total de los Impuestos
Trasladados y/o Retenidos.
```
```
Fundamento Legal: Artículo 29-A, primer párrafo, fracción VII, inciso a), primer y
segundo párrafo del Código Fiscal de la Federación, Anexo 20 versión 4.0 vigente
y “Anexo 20 Guía de llenado de los comprobantes fiscales digitales por Internet”
versión 4.0, publicada en el Portal del SAT.
```
**4. ¿En qué caso los campos condicionales del CFDI son de uso obligatorio?**


```
Los campos condicionales deberán informarse –serán obligatorios-
siempre que se registre información en algún otro campo que como
resultado de las reglas de validación contenidas en el estándar técnico y
precisadas en la Guía de llenado, obligue en consecuencia a que se registre
información en dichos campos condicionales.
```
```
Fundamento Legal: Anexo 20 versión 4.0 vigente y “Anexo 20 Guía de llenado de
los comprobantes fiscales digitales por Internet” versión 4.0, publicada en el
Portal del SAT.
```
**5. ¿Cómo se deben clasificar los productos y servicios de acuerdo con el**
    **catálogo publicado por el SAT (c_ClaveProdServ)?**

```
La clasificación del catálogo se integra de acuerdo con las características
comunes de los productos y servicios, y si están interrelacionados, la cual se
estructura de la siguiente manera:
```
```
 División: Se identifica por el primero y segundo dígito de la clave.
 Grupo: Se identifica por el tercero y cuarto dígito de la clave.
 Clase: Se identifica por el quinto y sexto dígito de la clave.
 Producto: Se identifica por el séptimo y octavo dígito de la clave.
```
```
Un ejemplo es la clave 10101502:
```
```
División Grupo Clase
```
```
Mercancía o
producto
```
```
Descripción
```
```
Dígitos
verificadores para
fines de análisis
```
```
Dígitos
agrupadores para
categorías de
productos
relacionados entre
sí
```
```
Dígitos para
identificar a los
productos que
comparten
características
comunes
```
```
Grupo de productos
o servicios
sustitutivos
```
```
Definición del
producto o servicio
```
```
10 10 15 02 Perros
```
(^)
Se debe registrar una clave que permita clasificar los conceptos del
comprobante, los cuales se deberán asociar a nivel Clase, es decir, cuando
los últimos dos dígitos tengan el valor cero ”0”, no obstante, se podrán
asociar a nivel Producto, siempre y cuando la clave esté registrada en el
catálogo.
_Fundamento Legal: Anexo 20 versión 4.0 vigente y “Anexo 20 Guía de llenado de
los comprobantes fiscales digitales por Internet” versión 4.0, publicada en el
Portal del SAT._


**6. ¿En los CFDI por anticipos se debe desglosar el IVA?**

```
Sí, se debe desglosar el IVA en las facturas que amparen anticipos cuando
el bien o producto a adquirir grave IVA.
```
```
Fundamento Legal: Artículos 1 y 1-B de la LIVA y “Anexo 20 Guía de llenado de los
comprobantes fiscales digitales por Internet” versión 4.0, publicada en el Portal
del SAT.
```
**7. El cliente se equivocó y pagó de más o indebidamente, ¿Se tiene que**

```
emitir una factura?
```
```
Si el cliente pagó de más o indebidamente y la cantidad que está en
demasía no se va a considerar como un anticipo, se deberá devolver al
cliente el importe pagado de más.
```
```
En el caso, de que la cantidad pagada de más o indebidamente se tome
como un anticipo, se deberá emitir el CFDI de conformidad con lo
establecido en el Apéndice 6 Procedimiento para la emisión de los CFDI en
el caso de anticipos recibidos.
```
```
Fundamento Legal: Artículo 29 del Código Fiscal de la Federación y “Anexo 20
Guía de llenado de los comprobantes fiscales digitales por Internet” versión 4.0,
publicada en el Portal del SAT.
```
**8. ¿Cómo se deben incluir los impuestos locales en el CFDI versión 4.0?**

```
Los impuestos locales se deben registrar en el “Complemento Impuestos
Locales”, publicado en el Portal del SAT.
```
```
Fundamento Legal: Artículo 29, segundo párrafo, fracción III del Código Fiscal de
la Federación, regla 2.7.1.8. de la Resolución Miscelánea Fiscal vigente y Apéndice
1 Notas Generales del “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```
**9. ¿El contribuyente receptor del CFDI tiene la obligación de validar a**
    **detalle las claves de producto/servicio de todas las facturas que reciba?**

```
No existe una obligación de revisarlas a detalle; la recomendación es que
se verifiquen que los datos asentados sean correctos y coincidan al menos
en términos generales con el bien o servicio de que se trate y la descripción
que del mismo se asiente en el propio comprobante.
```

```
Fundamento Legal: Anexo 20 versión 4.0 vigente y “Anexo 20 Guía de llenado de
los comprobantes fiscales digitales por Internet” versión 4.0, publicada en el
Portal del SAT.
```
**10. ¿Existen validadores de CFDI versión 4.0?**

```
El validador de forma y sintaxis de los CFDI que estaba disponible en el
Portal del SAT, dejó de dar servicio en mayo del 2017, toda vez que se
considera que un CFDI certificado cumple con las especificaciones técnicas
de estructura establecidas en el Anexo 20.
```
```
Si existe la necesidad de realizar las validaciones de forma y sintaxis a un
comprobante, se deberá obtener la herramienta con algún proveedor de
software.
```
**11. ¿Cuál es el método de pago que se debe registrar en el CFDI por el valor**

```
total de la operación en el caso de pago en parcialidades o pago
diferido?
```
```
Se debe registrar la clave PPD (Pago en parcialidades o diferido) del
catálogo c_MetodoPago publicado en el Portal del SAT.
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```
**12. ¿Qué sucede si clasifico de manera errónea en el CFDI la clave de los**
    **productos o servicios?**

```
En caso de que se asigne "erróneamente" la clave del producto o servicio
se debe reexpedir la factura para corregirlo.
```
```
Para clasificar los productos y servicios que se facturan, debe consultar el
Apéndice 3 del “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0 publicada en el Portal del SAT y puede
utilizarse la herramienta de clasificación publicada en el mismo Portal.
```
```
Fundamento Legal: Apéndice 3 Clasificación de Productos y Servicios del "Anexo
20 Guía de llenado de los comprobantes fiscales digitales por Internet” versión
4 .0, publicada en el Portal del SAT.
```
**13. ¿Cómo se deben registrar en los CFDI los conceptos exentos de**

```
impuestos?
```
```
En el Nodo:Traslados se debe expresar la información detallada del
impuesto, de la siguiente forma:
```

```
 Base para el cálculo del impuesto.
 Impuesto (Tipo de impuesto ISR, IVA, IEPS).
 Tipo factor (exento).
 No se deben registrar los campos TasaOCuota e Importe.
```
```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
**14. En el caso de que un CFDI haya sido pagado con diversas formas de**

```
pago ¿Qué forma de pago debe registrarse en el comprobante?
```
```
En el caso de aplicar más de una forma de pago en una transacción, los
contribuyentes deben incluir en este campo, la clave de forma de pago con
la que se liquida la mayor cantidad del pago. En caso de que se reciban
distintas formas de pago con el mismo importe, el contribuyente debe
registrar a su consideración, una de las formas de pago con las que se
recibió el pago de la contraprestación.
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```
##### 15. Si me realizan un depósito para garantizar el pago de las rentas en el

```
caso de un contrato de arrendamiento inmobiliario, ¿Se debe facturar
como un anticipo dicho depósito?
```
```
Si la operación de que se trata se refiere a la entrega de una cantidad por
concepto de garantía o depósito, es decir, la entrega de una cantidad que
garantiza la realización o cumplimiento de alguna condición, como sucede
en el caso del depósito que en ocasiones se realiza por el arrendatario al
arrendador para garantizar el pago de las rentas en el caso de un contrato
de arrendamiento inmobiliario, no estamos ante el caso de un anticipo.
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```
**16. ¿En qué casos se deberá emitir un CFDI por un anticipo?**

```
Estaremos ante el caso de una operación en dónde existe el pago de un
anticipo, cuando:
 No se conoce o no se ha determinado el bien o servicio que se va a
adquirir o el precio del mismo.
 No se conoce o no se ha determinado ni el bien o servicio que se va
a adquirir ni el precio del mismo.
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```

**17. Si tengo varias sucursales, pero mis sistemas de facturación se**
    **encuentran en la matriz ¿Qué código postal debo registrar en el campo**
    **lugar de expedición en el CFDI?**

```
En el caso de que se emita un comprobante fiscal en una sucursal, en dicho
comprobante se debe registrar el código postal de ésta,
independientemente de que los sistemas de facturación de la empresa se
encuentren en un domicilio distinto al de la sucursal.
```
```
Fundamento Legal: Artículo 29-A, primer párrafo, fracciones I y III del Código
Fiscal de la Federación, “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```
**18. Si solicito una factura de un gasto y tengo varias sucursales ¿Qué**
    **código postal debo solicitar se registre en el campo**
    **DomicilioFiscalReceptor en el CFDI?**

```
Se debe solicitar se registre el código postal que corresponda al domicilio
fiscal del receptor del comprobante, independiente si se cuenta o no con
sucursales.
```
```
Fundamento Legal: Artículo 29-A, primer párrafo, fracción IV del Código Fiscal de
la Federación, “Anexo 20 Guía de llenado de los comprobantes fiscales digitales
por Internet” versión 4.0, publicada en el Portal del SAT.
```
**19. ¿En qué apartado del CFDI se pueden expresar las penalizaciones o**
    **incumplimientos en el caso de contratos de obras públicas?**

```
Se podrán incluir en el nodo “Addenda”
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet versión” 4.0, publicada en el Portal del SAT.
```
**20. ¿Qué clave de unidad de medida se debe utilizar para facturar**

```
servicios?
```
```
La clave de unidad de medida dependerá del tipo de servicio y del giro del
proveedor, y ésta se identifica utilizando el catálogo c_ClaveUnidad
publicado en el Portal del SAT, puede haber más de una unidad de medida
aplicable a un servicio o producto, como se puede apreciar en los siguientes
ejemplos:
```
```
 Un servicio de transporte terrestre puede estar clasificado por
distancia (KMT), por peso transportado (KGM), por
pasajero/asiento (IE – persona), o por viaje (E54).
```

```
 Un servicio de hospedaje puede estar medido por
habitaciones (ROM), tiempo transcurrido (DAY), personas (IE).
 Los servicios administrativos y profesionales se pueden dar
por tiempo (HUR – hora, DAY, etc.), por actividades (ACT), por
grupos atendidos (10), por tiempo-hombre (3C – mes
hombre).
```
```
Fundamento Legal: Catálogos del CFDI versión 4.0, publicado en el Portal del
SAT.
```
**21. Si a mi cliente le otorgo un descuento sobre el total de una factura**

```
después de haberla emitido ¿qué tipo de CFDI debo emitir?
```
```
Se debe de emitir un CFDI de egresos.
```
```
Si el descuento lo aplican cuando se realiza la venta o prestación del
servicio, en el CFDI que se emita se puede aplicar el descuento a nivel
concepto.
```
```
Fundamento Legal: Artículo 29, penúltimo párrafo del Código Fiscal de la
Federación y 25, primer párrafo, fracción I de la Ley del Impuesto sobre la Renta.
```
**22. ¿Qué tipo de cambio podrán utilizar los integrantes del sistema**

```
financiero en la emisión del CFDI?
```
```
Podrán utilizar en tipo de cambio FIX, del último día del mes, de la fecha
de emisión o del día del corte del CFDI para operaciones en dólares de los
EUA, y en el caso de monedas distintas, el que corresponda conforme a la
tabla de Equivalencias la última que haya sido publicada por BANXICO.
```
**23. ¿Cuál es la clave de forma de pago que deben utilizar los integrantes**

```
del sistema financiero para la emisión de los CFDI?
```
```
La clave que deben utilizar es la “03” (Transferencia electrónica de fondos),
contenida en el catálogo c_FormaPago del Anexo 20.
```
```
Fundamento legal: Anexo 20 versión 4.0 vigente.
```
**24. En el campo “LugarExpedicion” del CFDI, ¿qué código postal deben de**
    **registrar los integrantes del sistema financiero?**

```
Deben registrar el código postal del domicilio fiscal de la institución
financiera.
```

```
Fundamento Legal: Artículo 29-A, primer párrafo, fracciones I y III del Código
Fiscal de la Federación y “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0, publicada en el Portal del SAT.
```
**25. ¿Cuál es la clave de unidad que deben utilizar por los servicios que**

```
prestan los integrantes del sistema financiero para la emisión de los
CFDI?
```
La clave de unidad que pueden utilizar es la “E48” (Unidad de servicio),

```
contenida en el catálogo c_ClaveUnidad del Anexo 20.
```
_Fundamento legal: Anexo 20 versión 4.0 vigente._

**26.¿Cuál es la clave de productos o servicios, que deben utilizar para**

```
clasificar los servicios que prestan los integrantes del sistema
financiero en la emisión de los CFDI?
```
La clave de productos o servicios que pueden utilizar es la “84121500”

```
(Instituciones bancarias), contenida en el catálogo c_ClaveProdServ del
Anexo 20.
```
Lo anterior, sin menoscabo de que los integrantes del sistema financiero

```
puedan, por la naturaleza del servicio prestado, clasificar éste de manera
particular.
```
_Fundamento legal: Anexo 20 versión 4.0 vigente y Artículo 7 de la Ley del
lmpuesto sobre la Renta._

**27. Para efectos de la emisión de CFDI a que se refiere la regla 2.7.1. 18 ., en**

```
los casos en los cuales éste deba emitirse por conceptos totalmente en
ceros, los integrantes del sistema financiero en la generación de los
mismos, podrán considerar lo siguiente:
```
Los integrantes del sistema financiero, podrán ingresar un cargo con valor

```
de un centavo o la cantidad que en su caso determinen por concepto de
”Servicios de Facturación“, con la clave de productos o servicios “84121500”
(Instituciones bancarias) y con clave de unidad “E48” (Unidad de servicio),
incluyendo en el mismo concepto un descuento por el mismo monto.
```
**28. ¿Las facturas se pueden pagar con bienes o servicios?**

No, no existe en el catálogo la forma de pago en especie o servicios,

```
derivado de que la persona que pretende pagar con bienes está realizando
la enajenación de un bien, por lo tanto, debe emitir un CFDI de ingresos
```

```
por ese bien que está enajenando, por otra parte, si la persona que
pretende pagar lo realiza con la prestación de un servicio, debe emitir un
CFDI por dicho servicio.
```
Tanto en el caso de la enajenación de bienes, como en la prestación de

```
servicios se considera que el cliente y el proveedor son el mismo
contribuyente, por lo tanto, se puede aplicar la forma de pago “17”
(Compensación).
```
_Fundamento legal: Artículo 14 del Código Fiscal de la Federación._

**29. ¿** **_Qué código postal se debe registrar en el CFDI cuando éste no se_**

```
encuentre en el Catálogo de código postal del Anexo 20?
```
```
En caso de que dentro del catálogo c_CodigoPostal, no se encuentre
contenida información del código postal, se debe registrar la clave del
código postal más cercano del lugar de expedición del comprobante fiscal.
```
```
Fundamento Legal: Artículo 29-A del Código Fiscal de la Federación y “Anexo 20
Guía de llenado de los comprobantes fiscales digitales por Internet” versión 4.0,
publicada en el Portal del SAT.
```
**30.** **_¿Qué forma de pago se debe registrar en el CFDI de egresos cuando_**

```
éste se emita por una devolución, descuento o bonificación
relacionado al CFDI de ingresos correspondiente, siempre que éste
último no haya sido pagado total o parcialmente?
```
```
Se puede registrar la clave "15" (Condonación) del catálogo c_FormaPago
publicado en el Portal del SAT.
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet versión” 4.0, publicada en el Portal del SAT.
```
**31.** **_Si otorgué una bonificación mediante una tarjeta de regalo, ¿qué_**

```
forma de pago debo registrar en el CFDI de egreso que ampara dicha
bonificación? y, ¿qué forma de pago se debe registrar en una factura
de ingreso cuando se reciba como medio de pago la tarjeta de regalo?
```
```
Se debe registrar en ambos casos la clave "01" (Efectivo) del catálogo
c_FormaPago publicado en el Portal del SAT.
```
```
Fundamento Legal: “Anexo 20 Guía de llenado de los comprobantes fiscales
digitales por Internet” versión 4.0.
```
**32.** **_¿Qué es un validador de factura electrónica?_**


```
Son aplicaciones informáticas o sistemas que confirman el cumplimiento
de la estructura y especificación técnica de un comprobante fiscal y en
algunos casos requisitos o elementos comerciales, definidos por el
receptor de un comprobante.
```
**33.** **_¿Los validadores comerciales son reconocidos por el SAT?_**

```
No, la única validación de facturas electrónicas con fundamento legal y
reconocimiento fiscal es la que realizan los Proveedores Autorizados de
Certificación (PAC) exclusivamente en el ejercicio de la certificación
"timbrado" que ejecutan al amparo de la autorización que el SAT le otorga.
```
**34.** **_¿Es necesario verificar en algún sistema de validación que una factura_**

```
electrónica certificada (timbrada) cumple con la estructura y
especificación técnica definida por el SAT?
```
```
No, cuando la factura ha sido ya certificada (timbrada) por el SAT, ya sea en
sus aplicaciones gratuitas de generación y certificación de facturas o a
través de un PAC, se considera que cumple con la estructura técnica
establecida en el Anexo 20 de la RMF vigente y por ende ha sido validada
por el SAT, por lo que no requiere de ser validada nuevamente en alguna
otra herramienta o validador tecnológico.
```
```
Fundamento Legal: Artículo 29, segundo párrafo, fracción IV del Código Fiscal de
la Federación.
```
**35.** **_¿La validación tecnológica que se aplique a un CFDI que ya ha sido_**

```
certificado (timbrado) por el SAT o alguno de sus proveedores
autorizados, distinta a las realizadas con los servicios de consulta de
folios que ofrece el SAT en función de lo dispuesto por el tercer párrafo
del artículo 29 del CFF y la regla 2.7.1.4 de la RMF vigente, tiene validez
fiscal?
```
```
La única validación tecnológica de una factura electrónica reconocida por
el SAT, es la que el propio SAT realiza directamente cuando la factura se
generó en sus aplicaciones tecnológicas o cuando esta fue certificada
(timbrada) por un PAC, cualquier otra validación tecnológica que un
contribuyente haga a una factura no tiene validez ante el SAT.
```
```
Fundamento Legal: Artículo 29 del Código Fiscal de la Federación y regla 2.7.1.4
de la RMF vigente.
```

**36.** **_¿El SAT reconoce o autoriza algún servicio, herramienta o sistema de_**

```
validación de facturas electrónicas ofrecido por terceros ajenos al
propio SAT?
```
```
No, la única validación de facturas electrónicas con fundamento legal y
reconocimiento fiscal es la que realizan los Proveedores autorizados de
certificación de CFDI (PAC), de esta forma cuando el PAC asigna a este
comprobante el sello digital del SAT, es decir lo "timbra", se está validando
el comprobante por el propio SAT a través del PAC, por lo cual los
contribuyentes que hagan uso del mismo solo requieren verificar que el
comprobante esta efectivamente sellado digitalmente por el SAT, esto a
través de alguna de las herramientas que ofrece el propio SAT, si
efectivamente esta “timbrado” por el SAT, el citado comprobante es válido
y no requiere de mayor validación tecnológica.
```
```
Fundamento legal: Artículo 29, segundo párrafo, fracciones IV y VI y 29 Bis del
CFF, reglas 2.7.1.4 y 2.7.2.5 de la RMF vigente.
```
**_37. El sistema de cómputo con el que genero mis CFDI, registra importes_**

```
con más de dos decimales por cada partida de la factura y con dos
decimales en la parte de totales. ¿Es válido generar comprobantes con
diferente número de decimales en las partidas y en los totales?
```
```
Sí es correcto; por cada concepto de la factura se puede utilizar de cero
hasta seis decimales como máximo y en los totales se debe redondear al
final del cálculo el resultado al número de decimales que soporta la
moneda.
```
```
Ejemplo:
Se emite una factura con moneda mexicana (MXN) con las siguientes
partidas donde base y tasa o cuota tienen cuatro decimales.
```
```
El monto del impuesto se calcula también con cuatro decimales; al
obtener el total del impuesto sumando las cuatro partidas se obtiene
```

```
0.7104; al redondear al final del cálculo el resultado al número de decimales
que soporta la moneda, en este caso son dos decimales, se llega a 0.71.
```
```
De esta manera los comprobantes son validados y timbrados sin problema
por el PAC.
```
```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
**_38. El sistema de cómputo con el que genero mis CFDI, “completa” a los_**

```
seis decimales permitidos campos como: Cantidad, Valor Unitario,
Importe (monto del impuesto), Descuento, Base; ¿es necesario rellenar
de ceros a la derecha en la parte fraccionaria, para completar los seis
decimales? ¿es válido omitir los ceros no significativos?
```
```
La validación del PAC para cada uno de los campos a reportar en el CFDI
debe de cumplir con que el número de decimales reportados sea menor o
igual al número de decimales especificados en el “Estándar del Anexo 20 y
sus complementos”. Esto permite rellenar de ceros a la derecha en la parte
fraccionaria para completar los seis decimales y también permite el omitir
los ceros no significativos; ambos criterios son aceptados.
```
```
Ejemplo:
```
```
Es importante ser consistentes al realizar las operaciones aritméticas con
el mismo número de decimales que hayan definido para los campos del
tipo importe.
```
```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
```
39. ¿En general cuales son las recomendaciones que sugiere el SAT, para
evitar un rechazo en la factura con respecto al tema de decimales?
```
```
Para evitar un rechazo de la factura se sugiere:
```
- Para los cálculos considerar el máximo número de decimales que
    permita el sistema que utilizan las empresas para generar su factura
    (hasta seis decimales como máximo).
        Los campos que permiten hasta seis decimales son los del tipo
          t_Importe, por ejemplo: Cantidad, Valor Unitario, Importe


```
(resultado de multiplicar cantidad por Valor Unitario),
Descuento, Base, Importe a nivel de impuestos.
```
- Ser consistentes al realizar las operaciones aritméticas con el mismo
    número de decimales que hayan definido para los campos del tipo
    importe del punto anterior.
- Redondear al final del cálculo y no antes, el resultado al número de
    decimales que soporta la moneda.
        En el caso del Importe de los Conceptos, el redondeo aplicará
          en el campo SubTotal del comprobante.
        En el caso de los Descuentos de los Conceptos, el redondeo
          aplicará en el campo Descuento del comprobante.
        En el caso de los Impuestos de los Conceptos, el redondeo
          aplicará en el resumen de Impuestos, en los campos Importe
          de los nodos Retenciones y Traslados (donde deben agruparse
          por Impuesto, TipoFactor y TasaOCuota).

```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
**_40. El sistema de cómputo con el que genero mis CFDI, en el campo de
TasaOCuota utiliza dos decimales ¿es válido utilizar sólo dos
decimales o se deben de reportar seis decimales?_**

```
Lo correcto es que el valor registrado debe corresponder a un valor, fijo o
de rango respectivamente, del catálogo c_TasaOCuota. Se deben usar
seis decimales de conformidad con dicho catálogo.
```
```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
**_41. En las validaciones para determinar el rango de los campos
numéricos con límites ¿es correcto que el límite inferior sea igual al
límite superior?_**

```
No es correcto; si el límite inferior es igual al superior; seguramente se
está aplicando mal el cálculo.
```
```
Ejemplo:
```
```
En el ejemplo anterior en el límite superior se realizó un redondeo
aritmético, siendo que se debe redondear hacia arriba.
```

```
En resumen, lo correcto es:
```
- El resultado de calcular el límite inferior truncarlo con el máximo número
    de decimales que permita el sistema (hasta seis decimales como
    máximo).
- El resultado de calcular el límite superior redondearlo hacia arriba con el
    máximo número de decimales que permita el sistema (hasta 6 decimales
    como máximo).

```
Ejemplo: moneda MXN, decimales dos, importe 924.224956
```
- Truncado del importe a dos decimales: 924.22
- Redondeado del importe hacia arriba: 924.23

```
Fundamento Legal: Anexo 20 versión 4.0 vigente.
```
```
42. Cuando se deba emitir un CFDI que sustituye a otro CFDI, ¿Qué debo
hacer?
```
```
Se debe actuar en este orden:
```
1. Se debe emitir el comprobante que contiene los datos correctos,

```
registrando la clave “04” (Sustitución de los CFDI previos) relacionando el
folio fiscal del comprobante que se sustituye.
```
2. Al registrar la solicitud de cancelación se debe seleccionar la opción "01"

```
(Comprobante emitido con errores con relación) e incluir el folio fiscal del
comprobante emitido en el paso 1.
```
3. Al enviar la solicitud de cancelación se validará si se requiere la aceptación

```
del receptor para llevar a cabo la cancelación.
```
```
Fundamento Legal: Artículo 29 - A, sexto párrafo del Código Fiscal de la Federación
y “Anexo 20 Guía de llenado de los comprobantes fiscales digitales por Internet”
versión 4.0, publicada en el Portal del SAT.
```

```
Apéndice 8 Caso de Uso Facturación de Anticipos
```
```
Disposiciones Generales
```
Todos los contribuyentes por los actos o actividades que realicen, por los ingresos
que perciban, por el pago de sueldos y salarios o por las retenciones de impuestos
que efectúen, deben emitir factura electrónica.

Se estará ante el caso de una operación en dónde existe el pago de un anticipo,

cuando se realice un pago en dónde:

```
a. No se conoce o no se ha determinado el bien o servicio que se va a
adquirir o el precio del mismo.
```
```
b. No se conoce o no se han determinado ni el bien o servicio que se va a
adquirir ni el precio del mismo.
```
No se considera anticipo:

```
A. La entrega de una cantidad por concepto de garantía o depósito, es
decir, la entrega de una cantidad que garantiza la realización o
cumplimiento de alguna condición, como sucede en el caso del
depósito que en ocasiones se realiza por el arrendatario al arrendador
para garantizar el pago de las rentas en el caso de un contrato de
arrendamiento inmobiliario.
```
```
B. En el caso de operaciones en las cuales ya exista acuerdo sobre el bien
o servicio que se va a adquirir y de su precio, aunque se trate de un
acuerdo no escrito, y el comprador o adquirente del servicio realiza el
pago de una parte del precio, estamos ante una venta en parcialidades
y no ante un anticipo.
```
(^)
**Fundamento Legal:** Artículos 29 y 29-A del Código Fiscal de la Federación, 17 de
la Ley del Impuesto sobre la Renta y 1-B de la Ley del Impuesto al Valor Agregado,
“Apéndice 6 Procedimiento para la emisión de los CFDI en el caso de anticipos
recibidos” del “Anexo 20 Guía de llenado de comprobantes fiscales digitales por
Internet” versión 4.0, publicada en el Portal del SAT.
**Planteamiento**
La empresa “Nueva Factura, S.A. de C.V.”, con RFC NUF150930AAA el 18 de julio
de 2022 recibe un anticipo de $10,000.00, se desconoce el bien que se va a
adquirir y el precio.
Hasta el 30 de julio se concreta la operación: Compra de una maquinaria para
bordados de playeras con un precio de $ 464,000.00.


```
Emisión de la Factura Electrónica
```
La empresa “Nueva Factura, S.A. de C.V.”, decide aplicar el procedimiento A del

“Apéndice 6 Procedimiento para la emisión de los CFDI en el caso de anticipos

recibidos” del “Anexo 20 Guía de llenado de comprobantes fiscales digitales por

Internet” versión 4.0, publicada en el Portal del SAT, es decir, -Facturación

aplicando anticipo con CFDI de egreso-, en el cual se deben emitir tres

comprobantes conforme a lo siguiente:

1. Emisión de la factura electrónica por el valor del anticipo recibido.
2. Emisión de la factura electrónica por el valor total de la operación.
3. Emisión de la factura electrónica de tipo “Egreso”.
**1. Emisión de la factura electrónica por el valor del anticipo.**

El 18 de julio se emite la factura por el monto del anticipo por el valor de

$10,000.00, el cual se recibe con cheque nominativo, quedando de la siguiente

forma:

**2. Emisión de la factura electrónica por el valor total de la operación.**

```
Nombre del Emisor: NUEVA FACTURA
RFC Emisor: NUF150930AAA
Clave de Regimen Fiscal: 601,General de Ley Personas Morales UUID: AAAca0c2-3ea4-0450-abe3-f05771737e69
Forma Pago:
Método pago:
Tipo de comprobante: I Ingreso
Lugar de expedición: 12068
Fecha y Hora de expedición: 2022-07-18 T00:00:00
Nombre del Receptor: VICTORIOSS LOPEZ AVILA
RFC Receptor: LOAV890607PY7
Código Postal del Receptor 14600
Régimen Fiscal del Receptor 612, Personas Físicas con Actividades Empresariales y Profesionales
Uso del CFDI: G03 Gastos en general
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave UnidadValor Unit ImporteDescuento ImpuestoObjeto Base Impuesto factorTipo Tipo tasaImporte
84111506 84569 1 ACT $8,620.69 $8,620.69 $0.00 02 $8,620.69 002 Tasa 0.160000$1,379.31
Descripción: Anticipo del bien o servcicio
Subtotal $8,620.69
Descuento $0.00
$1,379.31
Total $10,000.00
```
```
Comprobante Fiscal Digital por Internet
```
```
Total de impuestos trasladados:
```
```
02 Cheque nominativo
PUE Pago en una sola exhibición
```

El 30 de julio se concreta la operación, la cual consiste en la venta de una

maquinaria para bordados de playeras por un valor de $464,000.00, por lo que la

empresa “Nueva Factura, S.A. de C.V.”, recibe un cheque a su nombre por un valor

de $454,000.00 (Valor total de la operación $464,000.00 menos el valor del

anticipo $10,000.00) y emite la factura electrónica quedando de la siguiente

forma:

**_Nota:_** _Si al emitir la factura, no se recibe el pago de la diferencia entre el valor total de
la operación y el anticipo recibido en el campo “FormaPago” de la factura electrónica
que se emita por el valor total de la operación, se debe registrar la clave PPD (Pago en
parcialidades o diferido), y con posterioridad se debe emitir una factura con el
complemento de recepción de pagos por cada pago que se reciba._

**3. Emisión de la factura electrónica de tipo “Egreso”**

El 30 de julio se emite la factura electrónica de tipo egreso, es decir, el mismo día

de la emisión de la factura electrónica por el valor total de la operación, (aunque

no necesariamente debe ser el mismo momento o día) por un valor de

$10,000.00, quedando de la siguiente forma:

```
Comprobante Fiscal Digital por Internet
```
```
Nombre del Emisor: NUEVA FACTURA
RFC Emisor: NUF150930AAA
Clave de Regimen Fiscal: 601,General de Ley Personas Morales UUID: CCCca0c2-3ea4-0450-abe3-f05771737e69
Forma Pago:
Método pago:
Tipo de comprobante: I Ingreso
Lugar de expedición: 12068
Fecha y Hora de expedición: 2022-07-30 T00:00:00
```
```
Nombre del Receptor: VICTORIOSS LOPEZ AVILA
RFC Receptor: LOAV890607PY7 Tipo relación: 07 CFDI por aplicación de anticipo
Código Postal del Receptor 14600 CFDI relacionado: AAAca0c2-3ea4-0450-abe3-f05771737e69
Régimen Fiscal del Receptor 612, Personas Físicas con Actividades Empresariales y Profesionales
Uso del CFDI: I08 Otra maquinaria y equipo
Clav Prod. Serv. IdentificaciónNo Cantidad Clave Unidad Unidad Valor Unit Importe Descuento Impuesto Objeto Base Impuesto factorTipo Tipo tasa Importe
23121501 84569 1 EA Pieza $400,000.00 $400,000.00 $0.00 02 $400,000.00 002 Tasa 0.160000$64,000.00
Descripción: Maquina de bordados
Subtotal $400,000.00
Descuento $0.00
$64,000.00
Total $464,000.00
```
```
Total de impuestos trasladados:
```
```
02 Cheque nominativo
PUE Pago en una sola exhibición
```

##### Se emite la factura de Egreso para disminuir el valor del anticipo a efecto

##### de no duplicar los ingresos.

**_Nota_** _: No obstante, lo descrito, también puede optar por aplicar el procedimiento “B
Facturación aplicando anticipo con remanente de la contraprestación” el cual puede
consultar en el Apéndice 6 de esta guía._

```
Nombre del Emisor: NUEVA FACTURA
RFC Emisor:Clave de Regimen Fiscal: NUF150930AAA601,General de Ley Personas Morales UUID: d21ca0c2-3ea4-0450-abe3-f05771737e69
Forma Pago:
Método pago:
Tipo de comprobante:Lugar de expedición: E Egreso 12068
Fecha y Hora de expedición: 2022-07-30 T00:00:00
Nombre del Receptor: VICTORIOSS LOPEZ AVILA
RFC Receptor: LOAV890607PY7
Código Postal del Receptor 14600 Tipo relación: 07 CFDI por aplicación de anticipo
Régimen Fiscal del Receptor 612, Personas Físicas con Actividades Empresariales y Profesionales
Uso del CFDI: G02 Devoluciones, descuentos o bonificaciones. CFDI relacionado: CCCca0c2-3ea4-0450-abe3-f05771737e69
Clav Prod. Serv.
```
```
No
Identificación Cantidad UnidadClave UnidadValor Unit Importe Descuento Impuesto Objeto Base ImpuestofactorTipo Tipo tasaImporte
84111506 84569 1 ACT $8,620.69 $8,620.69 $0.00 02 $8,620.69 002 Tasa 0.16 $1,379.31
Descripción: Aplicación de anticipo
SubtotalDescuento $8,620.69$0.00
$1,379.31
Total $10,000.00
```
```
Comprobante Fiscal Digital por Internet
```
```
Total de impuestos trasladados:
```
```
30 Aplicación de anticipo
PUE Pago en una sola exhibición
```

```
Apéndice 9 Caso de Uso Facturación por contratos de obra pública
```
##### Disposiciones generales

Todos los contribuyentes por los actos o actividades que realicen, por los ingresos
que perciban, por el pago de sueldos y salarios o por las retenciones de impuestos
que efectúen, deben emitir factura electrónica.

##### Planteamiento

La empresa “Nueva Factura, S.A. de C.V.”, con RFC NUF150930AAA el 10 de agosto
de 20 22 recibe un anticipo de $348,000.00, para realizar una obra, de la cual se
desconoce el tipo y el precio de la misma.

Hasta el 1 de septiembre de 20 22 se concreta la operación: Obra de una carretera
(México-Puebla) con un precio de $ 1,160,000.00.

Por cada avance de obra se envía una estimación la cual se pactó para este

ejemplo que será por mes vencido, por lo que al 01 de octubre la empresa “Nueva

Factura, S.A. de C.V.” solicita el pago presentando la estimación de obra por el

avance que se tuvo en la misma durante el mes de septiembre y el valor de esta

estimación es de $100,000.00, mismo que se paga el mismo día. Al recibir el pago,

la empresa “Nueva Factura, S.A. de C.V.” emite el CFDI con complemento para

recepción de pagos.

El 3 de octubre de 20 22 el dueño (ente público) visita la obra y verifica que la

carretera no cubre la medida acordada, por lo que se le otorga un descuento del

10% sobre el precio de la obra.

El 1 de noviembre de 20 22 la empresa “Nueva Factura, S.A. de C.V.” solicita el pago

de la segunda estimación de obra por el avance que se tuvo en la misma durante

el mes de octubre, el valor de esta estimación es de $78,000.00, mismo que se

paga el mismo día con transferencia electrónica de fondos. Al recibir el pago, la

empresa “Nueva Factura, S.A. de C.V.” emite el CFDI con Complemento para

recepción de Pagos.

El 8 de noviembre de 20 22 el dueño de la obra (ente público) pide que se le

agreguen detalles a la obra por lo que ésta aumenta su valor en un 25% sobre el

precio de la obra que originalmente se pactó.

El primer día hábil de cada mes se vencieron y cobraron las estimaciones de los
meses vencidos, por lo que cada estimación se cobró de la siguiente forma:


```
 El 1 de diciembre de 20 22 , se cobró la estimación de obra por el avance que
se tuvo en la misma durante el mes de noviembre de 20 22 por un monto
de $200,000.00.
 El 1 de enero de 2023 , se cobró la estimación de obra por el avance que se
tuvo en la misma durante el mes de diciembre 20 22 por un monto de
$200,000.00.
 El 1 de febrero de 20 23 , se cobró la estimación de obra por el avance que
se tuvo en la misma durante el mes de enero 2 023 por un monto de
$200,000. 00.
 El 1 de marzo de 20 23 , se cobró la estimación de obra por el avance que se
tuvo en la misma durante el mes de febrero de 20 23 por un monto de
$208,000. 00.
```
**_Nota:_** Los impuestos locales que le apliquen de conformidad con las disposiciones
correspondientes, se deberán incluir en el “Complemento Impuestos Locales”, para este
ejemplo no se están contemplando los impuestos locales.

##### Emisión de la Factura Electrónica

La empresa “Nueva Factura, S.A. de C.V.”, decide aplicar el procedimiento “A” del

“Apéndice 6 Procedimiento para la emisión de los CFDI en el caso de anticipos

recibidos” del “Anexo 20 Guía de llenado de comprobantes fiscales digitales por

Internet”, es decir, -Facturación aplicando anticipo con CFDI de egreso-, en el cual

se deben emitir tres comprobantes conforme a lo siguiente:

1. Emisión de un CFDI por el valor del anticipo recibido.
2. Emisión de un CFDI por el valor total de la operación.
3. Emisión de un CFDI de tipo “Egreso”.

##### 1. Emisión de un CFDI por el valor del anticipo recibido.

El 10 de agosto de 20 22 , se emite la factura por el monto del anticipo por el valor

de $348,000.00, el cual se recibe con transferencia electrónica de fondos,

quedando de la siguiente forma:


##### 2. Emisión de un CFDI por el valor total de la operación.

El 01 de septiembre de 20 22 se concreta la operación, la cual consiste en la

realización de una carretera (México-Puebla) por un valor de $1,160,000.00, la cual

se pagará conforme avance la obra, por lo que se emite la factura electrónica

quedando de la siguiente forma:

```
Nombre del Emisor: NUEVA FACTURA Folio: C
RFC Emisor: NUF150930AAA Serie: 45
Clave de Regimen Fiscal: 601,General de Ley Personas Morales
Forma Pago:
Método pago:
Tipo de comprobante: I Ingreso
Lugar de expedición: 12068
Fecha y Hora de expedición: 2022-08-10 T00:00:00
```
```
Nombre del Receptor: EENNTT
RFC Receptor: ENT010101000
Código Postal del Receptor: 75450
Régimen Fiscal del Receptor: 601,General de Ley Personas Morales
Uso del CFDI: I01 Construcciones.
```
```
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Descripción Valor Unit Importe Impuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe
```
```
84111506 1 ACT bien o ServicoAnticipo del $300,000.00$300,000.00 02 $300,000.00 002 Tasa 0.160000 $48,000.00
```
```
Subtotal: $300,000.00
$48,000.00
Total: $348,000.00
```
```
Sello digital del Emisor:
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
```
```
Sello digital del SAT:
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
```
```
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-08-10T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
```
```
UUID: d21ca0c2-3ea4-3340-abe3-f05771835e96
Fecha y Hora del Certificado:
```
```
Comprobante Fiscal Digital por Internet
```
```
03 Transferencia electrónica de fondos
PUE Pago en una sola exhibición
```
```
Cadena original del complemento de certificación digital del SAT:
```
```
2022-08-10 T00:00:00
```
```
Total de impuestos trasladados:
```

Esta factura electrónica, por el valor total de la operación, se debe relacionar con

el CFDI que ampara el monto del anticipo otorgado el 10 de agosto de 20 22.

##### 3. Emisión de un CFDI de tipo “Egreso”

El 1 de septiembre de 20 22 se emite la factura electrónica de tipo egreso, es decir,
el mismo día de la emisión de la factura electrónica por el valor total de la
operación, (aunque no necesariamente debe ser el mismo momento o día) por
un valor de $348,000.00, quedando de la siguiente forma:

```
Nombre del Emisor: NUEVA FACTURA Folio: A
RFC Emisor: NUF150930AAA Serie: 102
Clave de Regimen Fiscal: 601,General de Ley Personas Morales
Forma Pago:
Método pago:
Tipo de comprobante: I Ingreso
Lugar de expedición: 12068
Fecha y Hora de expedición: 2022-09-01 T00:00:00 Tipo Relación 07 CFDI por aplicación de anticipo
UUID d21ca0c2-3ea4-3340-abe3-f05771835e96
Nombre del Receptor: EENNTT
RFC Receptor: ENT010101000
Código Postal del Receptor: 75450
Régimen Fiscal del Receptor: 601,General de Ley Personas Morales
Uso del CFDI: I01 Construcciones
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Descripción Valor Unit Importe ImpuestoObjeto Base Impuesto Tipo factor Tipo tasa Importe
72141000 1
```
```
E48
Unidad de
servicio
```
```
Construcción
carretera México-
Puebla
```
```
$1,000,000.00$1,000,000.00 02 $1,000,000.00 002 Tasa 0.160000 $160,000.00
```
```
Subtotal: $1,000,000.00
$160,000.00
Total: $1,160,000.00
Sello digital del Emisor:
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
Sello digital del SAT:
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
```
```
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-09-01T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d21ca0c2-3ea4-4652-abe3-f05771937e29
Fecha y Hora del Certificado: 2022-09-01 T00:00:00
```
```
Cadena original del complemento de certificación digital del SAT:
```
```
CFDI Relacionados
```
```
Comprobante Fiscal Digital por Internet
```
```
Total de impuestos trasladados:
```
```
99 Por definir
PPD Pago en parcialidades o diferido
```

Esta factura electrónica, se debe relacionar con el CFDI que ampara el valor total

de la operación.

Se emite la factura de Egreso para disminuir el valor del anticipo a efecto de no

duplicar los ingresos.

**_Nota_** _: No obstante, lo descrito, también puede optar por aplicar el procedimiento “B
Facturación aplicando anticipo con remanente de la contraprestación” el cual puede
consultar en el Apéndice 6 de esta guía_

##### 4. Emisión del Recibo Electrónico de Pago (REP).

Por cada avance de obra se envía una estimación la cual se pactó, para este

ejemplo, que será cada mes, por lo que al 1 de octubre la empresa “Nueva Factura,

S.A. de C.V.” solicita el pago presentando la estimación de obra por el avance que

se tuvo en la misma, durante el mes de septiembre, el valor de esta estimación

es de $100,000.00, el cual se paga el mismo día con transferencia electrónica de

```
Nombre del Emisor: NUEVA FACTURA Folio: E
RFC Emisor: NUF150930AAA Serie: 32
Clave de Regimen Fiscal: 601,General de Ley Personas Morales
Forma Pago:
Método pago:
Tipo de comprobante: E Egreso
Lugar de expedición: 12068
Fecha y Hora de expedición: 2022-09-01 T00:00:00 Tipo Relación 07 CFDI por aplicación de anticipo
UUID d21ca0c2-3ea4-4652-abe3-f05771937e29
```
```
Nombre del Receptor: EENNTT
RFC Receptor: ENT010101000
Código Postal del Receptor: 75450
Régimen Fiscal del Receptor: 601,General de Ley Personas Morales
Uso del CFDI: G02 Devoluciones, descuentos o bonificaciones.
```
```
Clav Prod. Serv. No Identificación Cantidad UnidadClave Unidad Descripción Valor Unit Importe ImpuestoObjeto Base ImpuestoTipo factor Tipo tasa Importe
84111506 1 ACT Aplicación de anticipo $300,000.00$300,000.00 02 $300,000.00 002 Tasa 0.160000 $48,000.00
```
```
Subtotal: $300,000.00
$48,000.00
Total: $348,000.00
Sello digital del Emisor:
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
Sello digital del SAT:
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
```
```
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-09-01T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d21ca0c2-3ea4-4885-abe3-f05771737e69
Fecha y Hora del Certificado: 2022-09-01 T00:00:00
```
```
Cadena original del complemento de certificación digital del SAT:
```
```
CFDI Relacionados
```
```
Comprobante Fiscal Digital por Internet
```
```
Total de impuestos trasladados:
```
```
30 Aplicación de anticipo
PUE Pago en una sola exhibición
```

fondos. Al recibir el pago, la empresa “Nueva Factura, S.A. de C.V.” emite el CFDI

con complemento para recepción de pagos, quedando de la siguiente forma:

El importe del saldo anterior que se registró en el complemento, es la diferencia

entre el valor de la obra y el anticipo.

Este Recibo de Pago, se debe relacionar con el CFDI que ampara el valor total de
la operación_._

```
Comprobante Fiscal Digital por Internetcon Complemento para recepción de Pagos
Nombre del Emisor: NUEVA FACTURA
RFC Emisor: NUF150930AAA
Clave de Regimen Fiscal: 601,General de Ley Personas Morales
Forma Pago:
Método pago:
Tipo de comprobante:Lugar de expedición: P Pago
Fecha y Hora de expedición:^12068
2022-10-01 T00:00:00
Nombre del Receptor: EENNTT
RFC Receptor:Código Postal del Receptor: ENT010101000
Régimen Fiscal del Receptor:^75450
601,General de Ley Personas Morales
Uso del CFDI: CP01 Pagos
Clav Prod. Serv. No IdentificaciónCantidad UnidadClave Unidad DescripciónValor Unit Importe ImpuestoObjeto Base ImpuestoTipo factor Tipo tasa Importe
84111506 1 ACT Pago $0.00 $0.00 01
```
```
Subtotal: $0.00
Total: $0.00
```
```
Complemento
Fecha pago: 2022-10-01
Forma de pago: 03 Transferencia electrónica de fondos
Monto: $100,000.00
IdDocumento: d21ca0c2-3ea4-4652-abe3-f05771937e29
Num Parcialidad: 1
Importe saldo anterior: $812,000.00
Imp Pagado: $100,000.00
Impo Saldo Insoluto: $712,000.00
Sello digital del Emisor:
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNMqPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
```
```
Sello digital del SAT:
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
```
```
UUID: d21ca0c2-3ea4-8815-abe3-f05771726e72
Fecha y Hora del Certificado: 2022-10-01 T00:00:00
```
```
Cadena original del complemento de certificación digital del SAT:
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-10-01T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
```

##### 5. Emisión de la factura electrónica de tipo egreso por el descuento.

El 3 de octubre de 20 22 el dueño visita la obra (ente público) y verifica que la

carretera no cubre la medida acordada, por lo que se le otorga un descuento del

10% sobre el precio de la obra, quedando de la siguiente forma:

Este CFDI de egreso, se debe relacionar con el CFDI que ampara el valor total de

la operación.

##### 6. Emisión del Recibo Electrónico de Pago (REP).

El 1 de noviembre de 20 22 la empresa “Nueva Factura, S.A. de C.V.” solicita el pago

de la segunda estimación de obra por el avance que se tuvo en la misma durante

el mes de octubre, el valor de esta estimación es de $78,000.00, el cual se paga el

mismo día con transferencia electrónica de fondos. Al recibir el pago la empresa

“Nueva Factura, S.A. de C.V.” emite el CFDI con Complemento para recepción de

Pagos, quedando de la siguiente forma:

```
Nombre del Emisor:RFC Emisor: NUEVA FACTURANUF150930AAA Folio:Serie: H 96
Clave de Regimen Fiscal:Forma Pago: 601,General de Ley Personas Morales
Método pago:
Tipo de comprobante:Luga de expedición: E Egreso 12068 Moneda: MXN
Fecha y Hora de expedición: 2022-10-03 T00:00:00 Tipo RelaciónUUID 01 Nota de crédito de los documentos relacionados d21ca0c2-3ea4-4652-abe3-f05771937e29
Nombre del Receptor:RFC Receptor: EENNTTENT010101000
Código Postal del Receptor:Régimen Fiscal del Receptor: 75450
Uso del CFDI: 601,General de Ley Personas MoralesG02 Devoluciones, descuentos o bonificaciones.
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Descripción Valor Unit Importe Impuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe
84111506 1 ACT Descuento $100,000.00$100,000.00 02 $100,000.00 002 Tasa 0.160000 $16,000.00
Subtotal $100,000.00$16,000.00
Total $116,000.00
Sello digital del Emisor: mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
Sello digital del SAT: dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
```
```
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-10-03 T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d21ca0c2-3ea4-4340-abe3-f05771900e12
Fecha y Hora del Certificado: 2022-10-03 T00:00:00
```
```
Cadena original del complemento de certificación digital del SAT:
```
```
Comprobante Fiscal Digital por Internet
```
```
Total de impuestos trasladados:
```
```
15 CondonaciónPUE Pago en una sola exhibición
CFDI Relacionados
```

El importe del saldo anterior que se registró en el complemento, es la diferencia

entre el importe del saldo insoluto de la parcialidad 1 y el monto del descuento

del CFDI de egreso.

Este Recibo de Pago, se debe relacionar con el CFDI que ampara el valor total de

la operación.

##### 7. Emisión de la factura electrónica que complementa el precio original

##### de la operación.

El 8 de noviembre de 20 22 el dueño de la obra (ente público) pide que se le

agreguen detalles a la obra por lo que ésta aumenta su valor en un 25% sobre el

precio de la obra que originalmente se pactó, quedando de la siguiente forma:

```
Comprobante Fiscal Digital por Internetcon Complemento para recepción de Pagos
Nombre del Emisor:RFC Emisor: NUEVA FACTURANUF150930AAA
Clave de Regimen Fiscal:Forma Pago: 601,General de Ley Personas Morales
Método pago:
Tipo de comprobante:Lugar de expedición: P Pago 12068
Fecha y Hora de expedición: 2022-11-01 T00:00:00
Nombre del Receptor: EENNTT
RFC Receptor:Código Postal del Receptor: ENT010101000 75450
Régimen Fiscal del Receptor: 601,General de Ley Personas Morales
Uso del CFDI: CP01 Pagos
Clav Prod. Serv. No Identificación Cantidad UnidadClave Unidad DescripciónValor Unit Importe Impuesto Objeto Base ImpuestoTipo factor Importe
84111506 1 ACT Pago $0.00 $0.00 01
SubtotalTotal $0.00$0.00
```
```
ComplementoFecha pago: 2022-11-01
Forma de pago:Monto: 03 Transferencia electrónica de fondos$78,000.00
IdDocumento:Num Parcialidad: d21ca0c2-3ea4-4652-abe3-f05771937e29 2
Importe saldo anterior:Imp Pagado: $596,000.00$78,000.00
Impo Saldo Insoluto: $518,000.00
Sello digital del Emisor:
```
```
Sello digital del SAT:
```
```
UUID: d21ca0c2-3ea4-8853-abe3-f05771763e61
Fecha y Hora del Certificado: 2022-11-01 T00:00:00
```
```
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNMqPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
```
```
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChCTwf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
Cadena original del complemento de certificación digital del SAT:
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-11-01T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
```

Este CFDI se relaciona con el CFDI de ingreso que se emitió por el precio que

inicialmente se había pactado, ya que ambas suman el valor de la obra.

##### 8. Emisión de la factura electrónica por cada estimación de mes

##### vencido.

El primer día hábil de cada mes se vencieron y cobraron las estimaciones de los
meses vencidos, por lo que cada estimación se cobró de la siguiente forma:

```
 El 1 de diciembre de 20 22 , se cobró la estimación de obra por el avance que
se tuvo en la misma durante el mes de noviembre de 20 22 por un monto
de $200,000.00.
 El 1 de enero de 20 23 , se cobró la estimación de obra por el avance que se
tuvo en la misma durante el mes de diciembre 20 22 por un monto de
$200,000.00.
 El 1 de febrero de 20 23 , se cobró la estimación de obra por el avance que
se tuvo en la misma durante el mes de enero 20 23 por un monto de
$200,000. 00.
 El 1 de marzo de 20 23 , se cobró la estimación de obra por el avance que se
tuvo en la misma durante el mes de febrero de 20 23 por un monto de
$208,000. 00.
```
```
Nombre del Emisor:RFC Emisor: NUEVA FACTURANUF150930AAA Folio:Serie: A 215
Clave de Regimen Fiscal:Forma Pago: 601,General de Ley Personas Morales
Método pago:Tipo de comprobante: I Ingreso
Lugar de expedición:Fecha y Hora de expedición: 12068
2022-11-08 T00:00:00 Tipo RelaciónUUID 02 Nota de débito de los documentos relacionadosd21ca0c2-3ea4-4652-abe3-f05771937e29
Nombre del Receptor:RFC Receptor: EENNTTENT010101000
```
**Código Postal del Receptor:Régimen Fiscal del Receptor:** (^75450) 601,General de Ley Personas Morales
**Uso del CFDI:** I01 Construcciones
**Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Descripción Valor Unit Importe Impuesto Objeto Base ImpuestoTipo factor Tipo tasa Importe**
72141000 1 E48 Unidad de servicio
Complemento de
Construcción carretera $250,000.00$250,000.00^02 $250,000.00^002 Tasa 0.160000 $40,000.00
**Subtotal $250,000.00$40,000.00
Total $290,000.00
Sello digital del Emisor:** mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
**Sello digital del SAT:** dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-11-08T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d21ca0c2-3ea4-8450-abe3-f05771947e09
**Fecha y Hora del Certificado:** 2022-11-08 T00:00:00
**Cadena original del complemento de certificación digital del SAT:
CFDI Relacionados
Comprobante Fiscal Digital por Internet
Total de impuestos trasladados:**
99 Por definirPPD Pago en parcialidades o diferido


**Nota:** Por cada estimación vencida y pagada se debe emitir un CFDI con
Complemento para recepción de Pagos como se ejemplifica en los puntos 4 y 6
de este documento.


**_Apéndice 10 Caso de Uso Emisión del CFDI por donativos otorgados en_**

**_numerario o en especie y donativos globales en numerario o en especie_**

##### Disposiciones generales

Todos los contribuyentes por los actos o actividades que realicen, por los ingresos
que perciban, por el pago de sueldos y salarios o por las retenciones de impuestos
que efectúen, deben emitir factura electrónica de conformidad con el artículo 29
y 29-A del Código Fiscal de la Federación.

```
Planteamiento
```
**Donativo en numerario**

El señor Víctorioss López Ávila con RFC LOAV890607PY7, realiza un donativo a la
Fundación damnificados 0917 S.C. por $8,000.00 mediante transferencia electrónica de
fondos, por lo que la fundación emite el comprobante de tipo ingreso incorporando el
complemento de donativos, de la siguiente manera:

**Donativo en especie**

```
Donataria autorizada Nombre del Emisor: FUNDACIÓN DAMNIFICADOS 0917
RFC Emisor:Clave de Regimen Fiscal: FJP831022PW1603, Personas Morales con Fines no Lucrativos
Forma Pago:Método pago:
Tipo de comprobante:Lugar de expedición: I Ingreso 12068
Fecha y Hora de expedición: 2022-10-05 T00:00:00
Nombre del Receptor:RFC Receptor: VICTORIOSS LOPEZ AVILALOAV890607PY7 Moneda: MXN Tipo relación:
```
**Código Postal del Receptor:Régimen Fiscal del Receptor:** (^14600) 612, Personas Físicas con Actividades Empresariales y Profesionales CFDI relacionado:
**Uso del CFDI:** D04 Donativos
**Clav Prod. Serv. No Identificación Cantidad UnidadClave Unidad Valor Unit Importe Descuento ImpuestoObjeto BaseImpuestoTipo factorTipo tasaImporte
Descripción:**^84101600 Donativo para fomentar el desarrollo científico^1 M4 Valor monetario $8,000.00 $8,000.00^01
**SubtotalDescuento $8,000.00$0.00
Total $0.00$8,000.00
Número de autorización:Fecha de autorización:** *B400-05-08-2020-0052020-08-05
**Leyenda:
Sello digital del Emisor:** mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
**Sello digital del SAT:** dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-10-05T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d21ca0c2-3ea4-4501-abe3-f05771737e69
**Fecha y Hora del Certificado:** 2022-10-05 T00:00:00
**Complemento Donatarias Versión: 1.1
Este comprobante ampara un donativo, el cual será destinado por la donataria a los fines propios de su objeto social. En el caso de que los bienes donados hayan sido deducidos previamente para los efectos del impuesto sobre la renta, este donativo no es deducible. La reproducción no autorizada de este comprobante constituye un delito en los términos de las disposiciones fiscales.
*En el caso de que la donación se realice a una entidad pública en este campo se debrá registrar la palabra "Gobierno".
Cadena original del complemento de certificación digital del SAT:
Total de impuestos trasladados:
Comprobante Fiscal Digital por Internet**
03 "Transferencia electrónica de fondos"PUE Pago en una sola exhibición


El mismo contribuyente Victorioss López Ávila con RFC LOAV890607PY7, realiza
un donativo en especie a la Fundación damnificados 0917 S.C., de los siguientes
artículos: cinco colchones para dormir con valor unitario de $1,200.00 y 100 cobijas
térmicas con valor unitario de $1,500.00, por lo que la fundación emite el
comprobante de tipo ingreso incorporando el complemento de donativos de la
siguiente manera:

```
Donataria autorizada Nombre del Emisor: FUNDACIÓN DAMNIFICADOS 0917
RFC Emisor:Clave de Regimen Fiscal: FJP831022PW1603, Personas Morales con Fines no Lucrativos
Forma Pago:Método pago:
Tipo de comprobante:Lugar de expedición: I Ingreso
Fecha y Hora de expedición:^12068 2022-10-05 T00:00:00
Nombre del Receptor:RFC Receptor: VICTORIOSS LOPEZ AVILALOAV890607PY7 Moneda: MXN Tipo relación:
Código Postal del Receptor: 14600 CFDI relacionado:
Régimen Fiscal del Receptor: 612, Personas Físicas con Actividades Empresariales y Profesionales
Uso del CFDI: D04 Donativos
Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Valor Unit Importe DescuentoImpuestoObjeto BaseImpuesto factorTipo Tipo tasaImporte Descripción
```
```
56101508 CIP0505 5 H87 Pieza $1,200.00 $6,000.00 01
```
```
Cinco
colchones de
poliuretano
24171701 CTR305 100 H87 Pieza $1,500.00 $150,000.00 01 100 cobijas térmicas
```
```
Subtotal $156,000.00
Descuento $0.00
$0.00
Total $156,000.00
```
```
Número de autorización: *B400-05-08-2020-005
Fecha de autorización: 2020-08-05
Leyenda:
```
```
Sello digital del Emisor:
mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNMqPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=
```
```
Sello digital del SAT: dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChC
Twf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
```
```
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2020-10-05T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8m
PnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d20ca0c2-3ea4-4503-abe3-f05771737e69
Fecha y Hora del Certificado:
```
```
Cadena original del complemento de certificación digital del SAT:
```
```
2022-10-05 T00:00:00
```
```
Complemento Donatarias Versión: 1.1
```
```
Este comprobante ampara un donativo, el cual será destinado por la donataria a los fines propios de su objeto social. En el caso de que los bienes donados hayan sido deducidos previamente para los efectos del impuesto sobre la renta, este donativo no es deducible. La reproducción no autorizada de este comprobante constituye un delito en los términos de las
disposiciones fiscales.
*En el caso de que la donación se realice a una entidad pública en este campo se debrá registrar la palabra "Gobierno".
```
```
Comprobante Fiscal Digital por Internet
```
```
12 "Dación en Pago"PUE Pago en una sola exhibición
```
```
Total de impuestos trasladados:
```

##### Donativo global en numerario

El Fideicomiso México te necesita, realizó un boteo para recolectar donativos

para los damnificados de los sismos del pasado mes de septiembre de 20 22 , por

lo cual se encuentra obligado a realizar una factura de ingresos con su respectivo

complemento que documentará de la siguiente manera:

##### Donativo global en especie

Algunos ciudadanos realizaron donaciones en especie al Fideicomiso México te
necesita, de los siguientes artículos:
 Cinco colchones para dormir de poliuretano con un valor unitario de
$1,200.00
 Cien cobijas para rescate con un valor unitario de $1,500.00
 Cinco cajas de conservas con un valor unitario de $100,000.00

```
Donataria autorizada Nombre del Emisor: FIDEICOMISO MÉXICO TE NECESITA
RFC Emisor:Clave de Regimen Fiscal: FME170920PW1603, Personas Morales con Fines no Lucrativos
Forma Pago:Método pago:
Tipo de comprobante:Lugar de expedición: I Ingreso 12068
Fecha y Hora de expedición: 2022-10-12 T00:00:00
Nombre del Receptor:RFC Receptor: PUBLICO EN GENERALXAXX010101000 Moneda: MXN Tipo relación:
```
**Código Postal del Receptor:Régimen Fiscal del Receptor:** (^12068) 616, Sin obligaciones fiscales CFDI relacionado:
**Uso del CFDI:** S01 Sin efectos fiscales.
**Clav Prod. Serv. IdentificaciónNo Cantidad UnidadClave Unidad Valor Unit Importe Descuento ImpuestoObjeto Base ImpuestofactorTipo Tipo tasa Importe Descripción**
01010101 AA12589 1 ACT Actividad $8,000.00 $8,000.00 01
reconstrucción zonas Donativo para
de septiembre de 2017afectadas sismo 7 y 19
01010101 AC48937 1 ACT Actividad $2,300.00 $2,300.00 01
reconstrucción zonas Donativo para
de septiembre de 2017afectadas sismo 7 y 19
01010101 BoteoA 30-09-17 1 ACT Actividad $900.00 $900.00 01
reconstrucción zonas Donativo para
de septiembre de 2017afectadas sismo 7 y 19
01010101 BoteoB 30-09-17 1 ACT Actividad $1,250.00 $1,250.00 01
reconstrucción zonas Donativo para
de septiembre de 2017afectadas sismo 7 y 19
**SubtotalDescuento** $12,450.00$0.00
**Total** $0.00$12,450.00
**Número de autorización:Fecha de autorización:** *B400-05-08-2020-0052020-05-08
**Leyenda:
Sello digital del Emisor:** mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y= **Sello digital del SAT:**
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChCTwf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-10-12T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d22ca0c2-3ea4-4584-abe3-f05771737e69
**Fecha y Hora del Certificado:** 2022-10-12 T00:00:00
**Cadena original del complemento de certificación digital del SAT:
Total de impuestos trasladados:
Complemento Donatarias Versión: 1.1
Este comprobante ampara un donativo, el cual será destinado por la donataria a los fines propios de su objeto social. En el caso de que los bienes donados hayan sido deducidos previamente para los efectos del impuesto sobre la renta, este donativo no es deducible. La reproducción no autorizada de este comprobante constituye un delito en los términos de
las disposiciones fiscales.
*En el caso de que la donación se realice a una entidad pública en este campo se debrá registrar la palabra "Gobierno".
Comprobante Fiscal Digital por Internet (Global de donativos)**
03 "Transferencia electrónica de fondos"PUE Pago en una sola exhibición


Por lo anterior, el Fideicomiso México te necesita, deberá emitir el comprobante
de la siguiente manera:

```
Donataria autorizada Nombre del Emisor: FIDEICOMISO MÉXICO TE NECESITA
RFC Emisor:Clave de Regimen Fiscal: FME170920PW1603, Personas Morales con Fines no Lucrativos
Forma Pago:Método pago:
Tipo de comprobante:Lugar de expedición: I Ingreso 12068
Fecha y Hora de expedición: 2022-10-10 T00:00:00
Nombre del Receptor:RFC Receptor: PUBLICO EN GENERALXAXX010101000 Moneda: MXN Tipo relación:
```
**Código Postal del Receptor:Régimen Fiscal del Receptor:** (^12068) 616, Sin obligaciones fiscales CFDI relacionado:
**Uso del CFDI:** S01 Sin efectos fiscales.
**Clav Prod. Serv. IdentificaciNo ón Cantidad UnidadClave Unidad Valor Unit Importe DescuentoImpuestoObjeto BaseImpuestofactorTipo Tipo tasa Importe Descripción**
56101508 CIP0505 5 H87 Pieza $1,200.00 $6,000.00 01 5 colchones de poliuretano
24171701 CTR305 100 H87 Pieza $1,500.00 $150,000.00 01 100 cobijas de algodón
50171904 CC8917 5 HBX Ciento de cajas $100,000.00$500,000.00 01 500 cajas de conservas
**SubtotalDescuento** $656,000.00$0.00
**Total** $0.00$656,000.00
**Número de autorización:Fecha de autorización:** *B400-05-08-2020-005
**Leyenda:** 2020-05-08
**Sello digital del Emisor:** mRoonZCKID8x9yYCommqomFp3sBFRS6mInJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr65TQ0PVNM
qPU0texN3NWXnYuwcoNbZwUg4Ceagl2D6nLkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y= **Sello digital del SAT:**
dhqumS8b4qY3jI46BpLMxbOkmh0oTpRdbX93UwNabz3WeSpmwuxMnOCLzShuFuvrKftWdN8xcIwYiS3ApDBX4ipChCTwf4pgkrgwyToKLmUMIe5t3VDwosyRcCLwd8iu9/2ofTb+GvLyM3vb7hNW6qTUigWu7cP++NYRovMfLc4=
||1.0|6E05BB78-09F7-40C4-943E-31E4D6379498|2022-10-10T00:00:00|sCoonZCKID9x9yZXommqomFp3sBFRS8mPnJQfsZi5bwJrjm8CJQNmWfyoyGAPNBhnZnFvqywUr97TQ0PVNMqPU0texN4NWXnYuwcoNbZwUg4Ceagl2D6n
LkGn8eUtnjYcaTaRhbfOEUcbbSCayY+sRyLn+Gjtbcl9jCe5FI0X9Y=|00001000000201748120||
UUID: d24ca0c2-3ea4-4894-abe3-f05771737e69
**Fecha y Hora del Certificado:** 2022-10-10 T00:00:00
**Cadena original del complemento de certificación digital del SAT:
Total de impuestos trasladados:
Complemento Donatarias Versión: 1.1
Este comprobante ampara un donativo, el cual será destinado por la donataria a los fines propios de su objeto social. En el caso de que los bienes donados hayan sido deducidos previamente para los efectos del impuesto sobre la renta, este donativo no es deducible. La reproducción no autorizada de este comprobante constituye un delito en los términos de las
disposiciones fiscales.
*En el caso de que la donación se realice a una entidad pública en este campo se debrá registrar la palabra "Gobierno".
Comprobante Fiscal Digital por Internet**
12 "Dación en Pago"PUE Pago en una sola exhibición


**_Apéndice 11 Instrucciones específicas de llenado en el CFDI aplicable a_**

**_operaciones individuales a Hidrocarburos, Petrolíferos y Servicios_**

**_relacionados_**

**Vigencia:** _Las disposiciones contenidas en el presente apéndice serán de_

_aplicación opcional a partir del 1 3 de enero de 2020, volviéndose de aplicación_

_obligatoria a partir del 1 de abril del mismo año._

En el apéndice 5 de la presente Guía se encuentra un ejemplo en el que se

describe cómo se puede aplicar un CFDI de egreso por un descuento, devolución

o bonificación de una operación que fue documentada en un CFDI.

Los nodos y campos no mencionados en este procedimiento, se deben registrar

en el comprobante fiscal conforme a las especificaciones generales contenidas

en esta guía.

```
Nodo:
Concepto
```
```
En este nodo se debe expresar la información detallada
de un bien o servicio descrito en el comprobante.
```
```
ClaveProdServ En este campo se debe registrar la clave que permita
clasificar los conceptos del comprobante como
productos o servicios correspondiente al hidrocarburo
o petrolífero; se deben utilizar las claves de los diversos
productos o servicios de conformidad con el catálogo
c_ClaveProdServ publicado en el Portal del SAT.
```
```
A continuación, se especifican las claves a utilizar:
```
```
I. Para el caso de productos:
```
```
a) Se debe registrar la clave “15101508” para el
petróleo en cualquiera de sus tipos (Súper-
ligero/Dulce, Súper-ligero/Semi-amargo, Súper-
ligero/Amargo, Ligero/Dulce, Ligero/Semi-
amargo, Ligero/Amargo, Mediano/Dulce,
Mediano/Semi-amargo, Mediano/Amargo,
Pesado/Dulce, Pesado/Semi-amargo,
Pesado/Amargo, Extra-pesado/Dulce, Extra-
pesado/Semi-amargo, Extra-pesado/Amargo).
```

b) Se debe registrar la clave **“ 15101514 ”** para la
**gasolina regular menor a 91 octanos**.

c) Se debe registrar la clave **“ 15101515 ”** para la
**gasolina premium mayor o igual a 91 octanos**.

##### d) Se debe registrar la clave “15101505” para el

```
diésel en cualquiera de sus tipos, diésel
automotriz (contenido mayor de azufre a 15
mg/kg y contenido máximo de azufre de 500
mg/kg), diésel de ultra bajo azufre (DUBA)
(contenido máximo de azufre de 15 mg/kg),
diésel marino (IFO 380), diésel industrial, diésel
```
##### agrícola, gasóleo doméstico.

e) Se debe registrar la clave **“ 15111512 ”** para el **gas
natural** en cualquiera de sus tipos, gas natural
vehicular comprimido, gas natural comprimido,
gas natural sin procesar, gas natural procesado,
etano, etc., o que tiene su origen en el gas natural
(por ejemplo: líquidos del gas natural y gasolina
natural).

f) Se debe registrar la clave **“15111511”** para el gas
natural licuado.

g) Se debe registrar la clave **“15111501”** para el
propano.

h) Se debe registrar la clave **“15101504”** para la
turbosina, no incluye al gasavión.

i) Se debe registrar la clave **“15101510”** para el
condensado (hidrocarburo).

j) Se debe registrar la clave **“15111510”** para el gas
licuado de petróleo.

k) Se debe registrar la clave **“15101701”** para el
combustóleo en cualquiera de sus tipos: fuel oil
de calefacción # 2, fuel oils pesados residuales #
4 ó # 6, combustóleo menor o igual a 1% de
azufre, combustóleo mayor a 1% y menor a 3% de
azufre, combustóleo mayor o igual a 3% y menor
a 4.4% de azufre o IFO 180.


l) Si la gasolina, diésel o turbosina, se encuentran
mezclados con un combustible no fósil, se debe
registrar la clave del combustible fósil que
compone dicha mezcla, es decir, las claves
**“ 15101514 ” o “ 15101515 ”** , para la mezcla de
gasolina con bioetanol, la clave **“15101505”** para
la mezcla de diésel con biodiesel y la clave
**“15101504”** para la mezcla de turbosina con
bioturbosina.

```
II. Para el caso de la prestación de servicios:
```
1. Se debe registrar la clave **“78131702”** para la
    actividad de Almacenamiento.
2. Se debe registrar la clave **“72141112”** para la
    actividad de Compresión de Gas Natural.
3. Se debe registrar la clave **“72141112”** para la
    actividad de Descompresión de Gas Natural.
4. Se debe registrar la clave **“72121517”** para la
    actividad de Licuefacción de Gas Natural.
5. Se debe registrar la clave **“72121517”** para la
    actividad de Regasificación de Gas Natural.
6. Se debe registrar la clave **“80141702”** para la
    actividad de Distribución.
7. Se debe registrar la clave **“71123003”** para la
    actividad de Extracción.
8. Se debe registrar la clave **“73101501”** para la
    actividad de Refinación.
9. Se debe registrar la clave **“73101502”** para la
    actividad de Procesamiento de Gas Natural.
10. Si la actividad es Transporte se debe registrar
    la clave que más se adecúe a tu operación:


```
 78101605 – Servicio de alquiler de
carrotanques de ferrocarril;
```
```
 78101701 – Servicio de transporte nacional
por buque;
```
```
 78101807 – Servicio de transporte de carga
de petróleo o químicos por carretera;
```
```
 78102101 – Transporte de productos
derivados del petróleo.
```
NoIdentificacion En este campo se debe registrar un número único y

```
consecutivo por descarga, mismo que puede tener un
máximo de 40 caracteres.
```
```
Ejemplo A:
```
```
Para la comercialización de petróleo, se generaría el
siguiente número de identificación:
```
```
NoIdentificacion = 232545389.
```
```
Nota: Tratándose de personas que enajenen
combustibles para vehículos marítimos, aéreos y
terrestres, como: gasolina, diésel, gas natural, o gas
licuado de petróleo combustóleo y turbosina, se debe
registrar el número de permiso vigente otorgado por la
Comisión Reguladora de Energía o la Secretaría de
Energía, seguido de un guion medio para registrar un
número único y consecutivo por descarga, mismo que
puede tener un máximo de 40 caracteres.
```
```
Los permisos cuya nomenclatura se deben registrar,
según corresponda, son los siguientes:
```
```
 Comercialización de hidrocarburos, petrolíferos y
petroquímicos (combinados) - (Con nomenclatura
tipo: H/XXXXX/COM/AAAA)
```
(^)
 Comercialización de petrolíferos, petroquímicos
y bioenergéticos - (Con nomenclatura tipo:
H/XXXXX/COM/AAAA)


```
 Comercialización de gas natural - (Con
nomenclatura tipo: G/XXXXX/COM/GN/AAAA)
```
```
 Comercialización de Gas Licuado de Petróleo -
(Con nomenclatura tipo: LP/XXXXX/COM/AAAA)
```
```
 Expendio en aeródromos - (Con nomenclatura
```
tipo: PL/XXXXX/EXP/AE/AAAA)^

```
 Expendio al público de gasolinas y diésel
mediante estación de servicio - (Con nomenclatura
tipo: PL/XXXXX/EXP/ES/AAAA)
```
```
 Expendio al público de gasolinas y diésel
mediante estación de servicio multimodal - (Con
nomenclatura tipo: PL/XXXXX/EXP/ES/MM/AAAA)
```
```
 Expendio al público de gas natural mediante
estación de servicio con fin específico - (Con
nomenclatura tipo: G/XXXXX/EXP/ES/FE/AAAA)
```
```
 Expendio al público de gas natural mediante
estación de servicio multimodal - (Con
nomenclatura tipo: G/XXXXX/EXP/ES/MM/AAAA)
```
```
 Expendio al Público de Gas Licuado de Petróleo
mediante Estación de Servicio con fin Específico -
(Con nomenclatura tipo: LP/XXXXX/EXP/ES/AAAA)
```
```
 Distribución de Gas Licuado de Petróleo por
medio de Auto-Tanques - (Con nomenclatura tipo:
LP/XXXXX/DIST/AUT/AAAA).
```
```
 Distribución de Gas Licuado de Petróleo
mediante Planta de Distribución - (Con
nomenclatura tipo: LP/XXXXX/DIST/PLA/AAAA)
```
```
 Distribución de Gas Licuado de Petróleo por
medio de vehículos de reparto - (Con nomenclatura
tipo: LP/XXXXX/DIST/REP/AAAA)
```
```
 Distribución de gas natural por medio de ductos
```
- (Con nomenclatura tipo: G/XXXXX/DIS/AAAA)


```
 Distribución de gas natural por medios distintos
a ductos - (Con nomenclatura tipo:
G/XXXXX/DIS/OM/AAAA)
```
```
Ejemplo A:
```
```
Para expendio al público de Gas Licuado de Petróleo,
mediante estación de servicio con fin específico, se
generaría el siguiente número de identificación:
```
```
NoIdentificacion = LP/00751/EXP/ES/2016- 105012365.
```
```
Si el tipo de comprobante es de Egreso, en este campo
se debe expresar la información del campo
NoIdentificacion del comprobante fiscal que se está
relacionado en este CFDI.
```
Cantidad En este campo se debe registrar la cantidad de bienes

```
o servicios que corresponden a cada concepto.
```
```
Nota : Cuando se haya realizado la medición en una
unidad de medida distinta, deberá realizarse la
conversión correspondiente.
```
```
Ejemplo:
```
```
Cantidad= 10.236
```
ClaveUnidad En este campo se debe registrar la clave de unidad de

```
medida estandarizada de conformidad con el catálogo
c_ClaveUnidad publicado en el Portal del SAT, aplicable
para la cantidad expresada en cada concepto
correspondiente al producto conforme a lo siguiente:
```
```
Producto Unidad de medida c_ClaveUnidad
```
```
Petróleo y
condensados
```
```
Barril BLL
```

```
Gas natural
(Contratistas
y
Asignatarios)
```
```
Pie cúbico FTQ
```
```
Gas natural Metro cúbico MTQ
```
```
Gasolinas Litros LTR
```
```
Diésel Litros LTR
```
```
Turbosina Litros LTR
```
```
Combustóleo Litros LTR
```
```
Gas licuado
de petróleo
```
```
Litros LTR
```
```
Propano Litros LTR
```
```
Ejemplo:
```
```
ClaveUnidad= FTQ
```
```
Referente a los servicios se debe registrar la clave
correspondiente conforme al c_ClaveUnidad publicado
en el Portal del SAT.
```
```
Nota: Cuando se haya realizado la medición en una
unidad de medida distinta, deberá realizarse la
conversión correspondiente.
```
Unidad En este campo se debe registrar la unidad de medida

```
del bien o servicio propio de la operación del emisor,
aplicable para la cantidad expresada en cada concepto.
```
```
La unidad para los siguientes productos debe
corresponder con la ClaveUnidad del catálogo
c_ClaveUnidad conforme lo siguiente:
```
```
Producto Unidad de Medida
```

```
Petróleo y condensados Barril
```
```
Gas natural (Contratistas y
Asignatarios)
```
```
Pie cúbico
```
```
Gas natural Metro cúbico
```
```
Gasolinas Litros
```
```
Diésel Litros
```
```
Turbosina Litros
```
```
Combustóleo Litros
```
```
Gas licuado de petróleo Litros
```
```
Propano Litros
```
```
Ejemplo:
```
```
Unidad= Pie cúbico
```
Descripcion En este campo se debe registrar la descripción del bien

```
o servicio por cada concepto.
```
1. Si se registró la clave **“15101508”** en el campo
    ClaveProdServ se debe registrar la siguiente
    descripción, según corresponda:

```
 “Súper-ligero/Dulce”,
 “Súper-ligero/Semi-amargo”,
 “Súper-ligero/Amargo”,
 “Ligero/Dulce”,
 “Ligero/Semi-amargo”,
 “Ligero/Amargo”,
 “Mediano/Dulce”,
 “Mediano/Semi-amargo”,
 “Mediano/Amargo”,
 “Pesado/Dulce”,
 “Pesado/Semi-amargo”,
 “Pesado/Amargo”,
 “Extra-pesado/Dulce”,
 “Extra-pesado/Semi-amargo”,
```

```
 “Extra-pesado/Amargo”.
```
2. Si se registró la clave **“ 15101514 ”** , **“ 15101515 ”,**
    **“15101505” o “15101504”** cuando la gasolina, el
    diésel o la turbosina se encuentran mezclados
    con biocombustibles, se debe registrar el
    porcentaje de cada producto que componga la
    mezcla.

```
Ejemplo:
ClaveProdServ Descripción
```
(^15101505) Diésel 80%, Biodiesel 20%

3. Si se registró la clave **“78131702”** en el campo
    ClaveProdServ, correspondiente a
    Almacenamiento se debe registrar el
    hidrocarburo o petrolífero objeto del
    almacenamiento de que se trate.

```
Ejemplo :
Descripcion= Almacenamiento de diésel
```
4. Si se registró la clave **“72141112”** en el campo
    ClaveProdServ, se debe registrar la siguiente
    descripción, según corresponda:

```
 “Compresión de Gas Natural”,
 “Descompresión de Gas Natural”.
```
5. Si se registró la clave **“72121517”** en el campo
    ClaveProdServ, se debe registrar la siguiente
    descripción, según corresponda:

```
 “Licuefacción de Gas Natural”,
 “Regasificación de Gas Natural”.
```
6. Si se registró la clave **“80141702”** en el campo
    ClaveProdServ, correspondiente a Distribución
    se debe registrar el hidrocarburo o petrolífero
    objeto de distribución de que se trate.


```
Ejemplo :
Descripcion= Distribución de Gas L.P. por
Autotanque
```
7. Si se registró la clave **“71123003”** en el campo
    ClaveProdServ, correspondiente a Extracción, se
    debe registrar el hidrocarburo objeto de
    extracción de que se trate.

```
Ejemplo :
Descripcion= Extracción de Petróleo y Gas
Natural
```
8. Si se registró la clave **“73101501”** en el campo
    ClaveProdServ, correspondiente a Refinación, se
    debe registrar el hidrocarburo objeto de
    refinación de que se trate.

```
Ejemplo :
Descripcion= Refinación de petróleo
```
9. Si se registró la clave **“73101502”** en el campo
    ClaveProdServ, correspondiente al
    Procesamiento de Gas Natural, se debe registrar
    “Procesamiento de Gas Natural”.
10. Si se registraron las claves **“78101605”** ,
    **“78101701”** , **“78101807”** o **“78102101”** , en el
    campo ClaveProdServ, se debe registrar el
    hidrocarburo o petrolífero objeto del Transporte
    de que se trate.

```
Ejemplos:
Descripcion= Transporte de Turbosina por
Carrotanque
```
```
Descripcion= Transporte de Petróleo por
Buquetanque
```
```
Descripcion= Transporte de Combustóleo por
Autotanque
```

Descripcion= **Transporte de Gas Natural por**

**Ducto**


**_Control de cambios de la Guía de llenado del Comprobante Fiscal Digital_**

**_por Internet (CFDI)_**

```
Guía publicada en el Portal del SAT en Internet el 31 de diciembre de 2021
```
```
Revisión Fecha de
actualización
```
```
Descripción de la modificación o incorporación
```
```
1
0 8 de marzo del
2023
```
```
 Se precisa redacción en la Introducción.
 Se adiciona el párrafo en el campo FacAtrAdquirente
para precisar el número de dígitos que debe
contener el número de operación y se ajusta el
ejemplo.
 Se ajustan las etiquetas a los ejemplos de los campos
que conforman el Nodo: ACuentaTerceros.
 Se precisa fundamento legal en el campo
LugarExpedicion, y preguntas 1, 3, 17, 18, 24 , 34 y 36.
 Se actualizan imágenes de los numerales 3 de los
apéndices: 8 “Caso de Uso Facturación de Anticipos”
y apéndice 9 “Caso de Uso Facturación por contratos
de obra pública”.
 Se actualizan los permisos cuya nomenclatura se
deben registrar, dentro del atributo NoIdentificacion
y se adiciona en la Nota del campo NoIdentificacion
del Apéndice 11 a la Secretaría de Energía.
```

**_Control de cambios de la Guía de llenado del Comprobante Fiscal Digital_**

**_por Internet que ampara retenciones e información de pagos_**

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
1
0 8 de marzo
del 2023
```
```
Se ajustan etiquetas de los ejemplos de los campos:
LugarExpRetenc, RfcE, RegimenFiscalE,
NacionalidadR, RfcR, CurpR, DomicilioFiscalR,
NumRegIdTribR, Ejercicio, MontoTotRet,
ImpuestoRet, de la Guía de llenado del
Comprobante Fiscal Digital por Internet que
ampara retenciones e información de pagos.
```

