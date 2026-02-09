### ADR-003: Patrón de Diseño de Interfaz

**Estado:** Aceptado  
**Fecha:** 2024-12-12

**Contexto:**
Necesitamos un enfoque consistente para manejar múltiples áreas en una única aplicación Blazor.

**Decisión:**
Implementar:
1. Layouts específicos por área
2. Routing basado en prefijos (/shop/* y /workshop/*)
3. Autorización granular por área
4. Componentes compartidos para funcionalidad común

**Consecuencias:**
- Positivas:
  - Experiencia de usuario coherente
  - Mejor mantenibilidad
  - Reutilización efectiva de componentes
  - Control de acceso granular
- Negativas:
  - Mayor complejidad en la lógica de navegación
  - Necesidad de gestionar estado compartido
  - Posible duplicación de algunos componentes
