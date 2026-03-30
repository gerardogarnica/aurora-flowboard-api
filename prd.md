# Product Requirements Document (PRD)

## 1. Overview

**Product Name:** Flowboard
**Product Type:** Internal API for software project management
**Primary Users:**

* Analistas Funcionales
* Desarrolladores
* QA (Quality Assurance)
* Administradores de equipo

**Objective:**
Desarrollar una API robusta en .NET que permita gestionar proyectos de desarrollo de software mediante flujos de trabajo estructurados, facilitando la colaboración, trazabilidad y ejecución eficiente del trabajo.

---

## 2. Problem Statement

Los equipos de desarrollo requieren herramientas para:

* Gestionar tareas y requerimientos
* Coordinar trabajo entre roles distintos
* Tener visibilidad del progreso
* Mantener trazabilidad de cambios

Las soluciones existentes (Jira, Trello, Notion) pueden ser:

* Costosas
* Sobredimensionadas
* Poco adaptadas a necesidades internas

**Flowboard busca cubrir este gap con una solución interna, flexible y extensible.**

---

## 3. Goals & Objectives

### 3.1 Goals

* Centralizar la gestión de proyectos de desarrollo
* Estandarizar workflows de trabajo
* Mejorar la colaboración entre equipos
* Permitir trazabilidad completa del ciclo de vida de tareas

### 3.2 Non-Goals (MVP)

* No incluir UI (solo API inicialmente)
* No incluir integraciones externas complejas
* No implementar analítica avanzada en la primera fase

---

## 4. Core Features (MVP Scope)

### 4.1 Project Management

* Crear, editar y eliminar proyectos
* Configurar información básica del proyecto
* Asociar usuarios a proyectos

---

### 4.2 User & Role Management

* Gestión de usuarios
* Asignación de roles (Admin, Analyst, Developer, QA)
* Control de acceso basado en roles (RBAC)

---

### 4.3 Work Item Management

* Crear y gestionar WorkItems
* Tipos: Historia, Bug, Tarea Técnica, Investigación
* Asignación a usuarios
* Prioridad y estimación

---

### 4.4 Workflow Management (Flow Engine)

* Definición de estados (FlowStates)
* Transiciones entre estados
* Validaciones por rol

Ejemplo:
Backlog → In Progress → Code Review → QA → Done

---

### 4.5 Boards (Kanban View)

* Visualización de WorkItems por estado
* Agrupación por proyecto
* Ordenamiento y filtrado

---

### 4.6 Comments & Collaboration

* Comentarios en WorkItems
* Menciones a usuarios
* Historial de conversación

---

### 4.7 Time Tracking (Basic)

* Estimación inicial
* Registro de tiempo trabajado

---

### 4.8 Audit & History

* Registro de cambios en WorkItems
* Historial de estados
* Cambios de asignación

---

### 4.9 Notifications (Basic)

* Eventos internos (Domain Events)
* Base para futuras notificaciones

---

## 5. User Stories (Ejemplos)

* Como desarrollador, quiero ver mis tareas asignadas para priorizar mi trabajo
* Como QA, quiero validar tareas antes de marcarlas como completadas
* Como analista, quiero documentar requerimientos dentro de una tarea
* Como administrador, quiero controlar quién accede a cada proyecto

---

## 6. Functional Requirements

* CRUD completo para todas las entidades principales
* Soporte para workflows configurables
* Validaciones de negocio en transiciones de estado
* Persistencia en base de datos relacional
* API RESTful

---

## 7. Non-Functional Requirements

### 7.1 Performance

* Respuesta < 300ms en operaciones comunes

### 7.2 Scalability

* Arquitectura preparada para crecimiento modular

### 7.3 Security

* Autenticación basada en JWT
* Autorización basada en roles

### 7.4 Maintainability

* Clean Architecture
* Separación de capas (Domain, Application, Infrastructure, API)

---

## 8. Technical Architecture

### 8.1 Stack

* .NET 10 (API)
* Entity Framework Core
* PostgreSQL

---

### 8.2 Architecture Style

* Clean Architecture
* Monolito modular
* Domain-Driven Design (DDD)

---

### 8.3 Core Modules

* Projects
* WorkItems
* Flows
* Boards
* Users

---

## 9. Domain Model (High-Level)

**Entities principales:**

* Project
* WorkItem
* Flow
* FlowState
* Board
* User
* Comment

---

## 10. API Design (High-Level)

### Endpoints clave

```
GET    /api/projects
POST   /api/projects

GET    /api/workitems
POST   /api/workitems

PATCH  /api/workitems/{id}/move

GET    /api/boards/{projectId}

POST   /api/comments
```

---

## 11. Success Metrics

* Adopción interna por equipos
* Reducción en uso de herramientas externas
* Mejora en visibilidad del trabajo
* Tiempo promedio de ciclo de tareas

---

## 12. Risks & Considerations

* Sobrediseño temprano del workflow
* Complejidad en manejo de permisos
* Necesidad futura de UI
* Posible integración con herramientas externas

---

## 13. Future Enhancements

* Integración con repositorios (Git)
* Automatización de workflows
* IA para priorización de tareas
* Métricas avanzadas (velocity, burndown)
* Notificaciones en tiempo real

---

## 14. Open Questions

* ¿Se permitirá configuración dinámica de workflows por proyecto?
* ¿Se manejarán múltiples tipos de boards?
* ¿Qué nivel de personalización necesitan los equipos?
* ¿Se integrará autenticación con sistemas corporativos (SSO)?

---

# Conclusion

Flowboard se posiciona como una API interna sólida, extensible y alineada a buenas prácticas de arquitectura, enfocada en mejorar la eficiencia y colaboración de equipos de desarrollo mediante un modelo centrado en el flujo de trabajo.
