# ADR-005: Sistema de Diseño DA Pintura

## Estado
Aceptado

## Fecha
2024-01-04

## Contexto
El sistema necesita mantener una identidad visual consistente a través de múltiples módulos (taller y tienda) mientras utiliza MudBlazor como framework de UI. Se requiere una guía de diseño que pueda ser utilizada tanto por desarrolladores como por sistemas de IA para generar interfaces coherentes.

## Decisión
Implementar un sistema de diseño estandarizado basado en Material Design con personalizaciones específicas para DA Pintura, definiendo:

1. Paleta de Colores:
   - Primary: #E53935 (Rojo DA)
   - Secondary: #757575
   - Surface: #FFFFFF
   - Background: #F5F5F5
   - Estados: Success (#4CAF50), Warning (#FF9800), Error (#E53935), Info (#2196F3)

2. Tipografía:
   - Familia principal: Roboto
   - Jerarquía definida: H1 (24px), H2 (20px), Body (14px), Caption (12px)

3. Componentes Base:
   - Botones con estilo pill (radio 20px)
   - Tarjetas con radio 8px y elevación sutil
   - Campos de entrada con altura 56px
   - Tablas con encabezados en #FAFAFA

4. Layout:
   - AppBar altura 64px
   - Drawer ancho 260px
   - Sistema de espaciado consistente (8px, 16px, 24px, 32px)

5. Patrones de Interacción:
   - Estados hover definidos
   - Feedback visual estandarizado
   - Comportamiento responsive documentado

## Consecuencias

### Positivas:
- Consistencia visual a través de toda la aplicación
- Reducción en tiempo de toma de decisiones de diseño
- Facilidad para generar nuevos componentes mediante IA
- Mejor experiencia de usuario
- Mantenimiento simplificado
- Proceso de desarrollo más eficiente
- Integración natural con MudBlazor

### Negativas:
- Necesidad de mantener documentación del sistema de diseño
- Posible limitación creativa en casos específicos
- Tiempo inicial requerido para configuración
- Curva de aprendizaje para nuevos desarrolladores
- Dependencia de MudBlazor para implementaciones

## Notas de Implementación
- El sistema se implementará mediante una clase CustomTheme en MudBlazor
- Se crearán componentes base reutilizables
- Se mantendrá documentación viva del sistema
- Se usarán constantes CSS para valores clave
- Se implementarán helpers para aplicar estilos consistentemente

## Cumplimiento
La implementación se considerará exitosa cuando:
1. Todos los componentes nuevos sigan el sistema
2. Los desarrolladores puedan implementar el diseño sin ambigüedad
3. La aplicación mantenga consistencia visual en todos los módulos

## Referencias
- Material Design Guidelines
- MudBlazor Documentation
- DA Pintura Brand Guidelines
- Diseños proporcionados por el cliente