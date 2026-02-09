### ADR-001: Estructura Base del Proyecto y Separación de Dominios

**Estado:** Aceptado  
**Fecha:** 2024-12-12 

**Contexto:** 
El sistema requiere manejar dos dominios principales (taller y tienda) que necesitan compartir cierta información pero mantener sus operaciones separadas.

**Decisión:** 
Implementar una arquitectura multicapa con separación clara de dominios y código compartido, manteniendo una única aplicación web con áreas separadas.

**Consecuencias:**
- Positivas:
  - Clara separación de responsabilidades
  - Facilidad de mantenimiento
  - Reducción de costos de hosting
  - Compartición eficiente de recursos
  - Simplificación del despliegue
- Negativas:
  - Mayor complejidad en el routing y la navegación
  - Necesidad de manejar permisos más granulares
  - Posible impacto en el rendimiento si un área tiene alta carga