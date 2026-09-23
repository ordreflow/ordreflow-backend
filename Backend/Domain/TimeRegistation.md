# Time Registration Domain Model

Dette dokument beskriver den nuværende Domain-model og den forventede forretningsadfærd for OrdreFlow.

Systemet er et single-company system. Der findes derfor ikke `Tenant`, `TenantId` eller virksomhedsskift i Domain.

## Overordnet model

Den centrale model består af tre Aggregate Roots:

```text
User
 │
 ├── ManagerId
 │
 ▼
Order
 └── Task[]

TimeEntry
 ├── EmployeeId
 ├── TaskId
 └── TimeEntryReview[]
```

Relationerne mellem objekterne er:

```text
User.ManagerId   -> User.UserId

Order.ManagerId  -> User.UserId

Task.OrderId     -> Order.Id

TimeEntry.EmployeeId -> User.UserId
TimeEntry.TaskId     -> Task.TaskId
```

`Order` ejer sine `Task`-objekter, og `TimeEntry` ejer sine `TimeEntryReview`-objekter.

Der bruges ikke `TimeSheet`, månedlige timesheets eller faste lønperioder som en del af Domain-modellen.

---

# Roller

## Employee

En medarbejder kan:

* Se de Orders, som tilhører medarbejderens manager-scope.
* Se Tasks på disse Orders.
* Oprette en TimeEntry på en Task, som tilhører medarbejderens manager.
* Se egne TimeEntries.
* Se egne TimeEntries for en valgt uge.
* Se den samlede tid for en valgt uge.
* Redigere egne åbne eller returnerede TimeEntries.
* Se årsagen, hvis en TimeEntry er returneret.
* Sende en rettet TimeEntry ind igen.

En medarbejder kan kun registrere tid på en Task, hvis den tilhørende Order har samme manager som medarbejderens `ManagerId`.

Den centrale adgangsregel er:

```text
employee.ManagerId == order.ManagerId
```

---

## Manager

En manager kan:

* Oprette Orders.
* Omdøbe og vedligeholde egne Orders.
* Lukke og genåbne egne Orders.
* Oprette Tasks på egne Orders.
* Ændre Tasks på egne Orders.
* Fjerne Tasks fra egne Orders.
* Se TimeEntries for medarbejdere i managerens scope.
* Acceptere TimeEntries.
* Returnere TimeEntries med en forklaring.
* Finalisere accepterede TimeEntries.

En manager kan kun administrere Orders, hvor:

```text
order.ManagerId == manager.UserId
```

En manager kan kun reviewe en medarbejders TimeEntry, hvis medarbejderen har manageren som sin manager:

```text
employee.ManagerId == manager.UserId
```

---

## Administrator

En administrator kan:

* Oprette brugere.
* Ændre brugerroller.
* Tilknytte medarbejdere til managers.
* Udføre managerfunktioner, hvis systemets adgangsregler tillader det.

Administratorens rettigheder til andre brugere kontrolleres af Domain-regler og Application-laget.

---

# Order

`Order` er den overordnede ordre eller sag i systemet.

En Order er et Aggregate Root og ejer sine Tasks.

Eksempel:

```text
Order: Kunde ABC - Produktionsanlæg

Tasks:
- Fejlfinding
- Udskift printkort
- Test anlæg
- Dokumentation
```

En Order indeholder blandt andet:

```text
OrderId
ManagerId
Name
Status
CreatedAt
ClosedAt
Tasks
```

En Order starter som:

```text
Open
```

og kan senere blive:

```text
Closed
```

En åben Order kan:

* Få nye Tasks.
* Få Tasks fjernet.
* Blive omdøbt.
* Modtage nye TimeEntries.

En lukket Order:

* Kan ikke få nye Tasks.
* Kan ikke få Tasks fjernet.
* Kan ikke omdøbes.
* Accepterer ikke nye TimeEntries.

Kun en aktiv manager kan administrere egne Orders.

En aktiv administrator kan administrere Orders efter systemets adgangsregler.

---

# Task

`Task` repræsenterer det konkrete arbejde, der udføres på en Order.

`Task` er en Entity inde i `Order`-aggregatet og har derfor ikke sit eget Aggregate Root.

En Task indeholder blandt andet:

```text
TaskId
OrderId
Title
Description
```

Relationen er:

```text
Order
 └── Task
      └── OrderId
```

En Task tilhører præcis én Order.

Eksempel:

```text
Order: Kunde ABC - Produktionsanlæg

Task:
  "Udskift printkort"
```

Tasken kan ikke eksistere som en del af flere Orders.

Når en Task oprettes gennem:

```text
Order.AddWorkItem(...)
```

oprettes Tasken og tilknyttes Orderens `OrderId`.

I Domain-koden bruges `Task` som navnet på entiteten. Selvom navnet kan kollidere med `System.Threading.Tasks.Task`, håndteres dette med namespace alias, hvor det er nødvendigt.

---

# Manager-scope

Der gemmes ikke individuelle medarbejdertildelinger på Task.

I stedet bestemmes medarbejderens adgang gennem relationen:

```text
User.ManagerId
        │
        ▼
Order.ManagerId
```

Eksempel:

```text
Manager Mads
     │
     ├── Peter
     └── Anna

Order 100
ManagerId = Mads

Task 42
OrderId = Order 100
```

Både Peter og Anna kan arbejde på Tasks på Order 100, fordi deres `ManagerId` matcher Orderens `ManagerId`.

Der findes derfor ikke:

```text
Task.AssignedEmployeeIds
```

eller tilsvarende individuelle tildelinger på Task.

---

# TimeEntry

`TimeEntry` er en Aggregate Root og repræsenterer selve tidsregistreringen.

En TimeEntry tilhører en medarbejder og peger på den konkrete Task, som arbejdet blev udført på.

En TimeEntry indeholder:

```text
TimeEntryId
EmployeeId
TaskId
Date
Hours
Comment
Status
Reviews
```

Relationerne er:

```text
TimeEntry.EmployeeId -> User.UserId
TimeEntry.TaskId     -> Task.TaskId
Task.OrderId         -> Order.Id
```

TimeEntry behøver derfor ikke selv indeholde:

```text
OrderId
```

fordi Order kan findes gennem Task:

```text
TimeEntry
    │
    └── TaskId
          │
          ▼
        Task
          │
          └── OrderId
                │
                ▼
              Order
```

TimeEntry skal heller ikke indeholde:

```text
TimeSheetId
Year
Month
PayrollPeriodId
```

---

# Oprettelse af TimeEntry

`TimeEntry.Create()` er ansvarlig for at oprette selve TimeEntry-domainobjektet og validere de regler, der kun vedrører TimeEntry.

Eksempel:

```csharp
var timeEntryResult = TimeEntry.Create(
    employee.UserId,
    task.TaskId,
    command.Date,
    command.Hours,
    command.Comment);
```

`TimeEntry.Create()` kontrollerer blandt andet:

* EmployeeId findes.
* TaskId findes.
* Datoen er gyldig.
* Timerne er større end 0.
* Timerne overstiger ikke 24 timer.
* Kommentaren overholder maksimal længde.

Hvis oprettelsen lykkes, returneres:

```text
Result<TimeEntry>
```

med en TimeEntry i status:

```text
Draft
```

`TimeEntry.Create()` gemmer ikke noget i databasen.

Persistence sker først senere gennem Repository og Unit of Work.

---

# Registrering af tid

Registrering af tid involverer flere Aggregate Roots og koordineres derfor af `TimeRegistrationDomainService`.

Processen er:

```text
1. Frontend sender TimeEntry-request.
2. WebAPI mapper request til Command.
3. Application sender Command gennem ICommandDispatcher.
4. Handler finder den aktuelle User.
5. Handler finder den relevante Order og Task.
6. Handler kalder TimeEntry.Create().
7. TimeEntry oprettes i memory med status Draft.
8. TimeRegistrationDomainService validerer registreringen.
9. Order.CanRegisterTime() kontrollerer Order-specifikke regler.
10. Repository tilføjer TimeEntry.
11. UnitOfWork gemmer ændringen i databasen.
```

Flowet kan illustreres således:

```text
WebAPI
   │
   ▼
Command
   │
   ▼
CreateTimeEntryHandler
   │
   ├── Hent User
   │
   ├── Hent Order/Task
   │
   ├── TimeEntry.Create()
   │       │
   │       ▼
   │    TimeEntry
   │    Status = Draft
   │
   ├── TimeRegistrationDomainService.Register()
   │       │
   │       ▼
   │    Order.CanRegisterTime()
   │
   ├── TimeEntryRepository.AddAsync()
   │
   └── UnitOfWork.SaveChangesAsync()
```

---

# Order.CanRegisterTime()

`Order.CanRegisterTime()` opretter ikke en TimeEntry.

Metoden validerer, om en allerede oprettet TimeEntry må registreres på Orderen.

Den kontrollerer blandt andet:

* Order er åben.
* Den valgte Task findes på Orderen.
* Medarbejderen har adgang til Orderens manager-scope.

Eksempel:

```csharp
var result = order.CanRegisterTime(
    timeEntry.TaskId,
    employee.UserId,
    employee.ManagerId);
```

Metoden returnerer enten:

```text
Result.Success()
```

eller:

```text
Result.Failure(...)
```

Selve TimeEntry'en er allerede oprettet af:

```text
TimeEntry.Create()
```

Hvis `CanRegisterTime()` returnerer en fejl, stopper Application-flowet, og TimeEntry'en gemmes ikke.

---

# Manager-scope-reglen

Den vigtigste regel ved tidsregistrering er:

```text
employee.ManagerId == order.ManagerId
```

For at kunne kontrollere dette skal backend kende:

```text
User
Order
Task
```

Et `TaskId` fra en HTTP-request er derfor ikke nok alene.

Application skal hente den relevante Order og kontrollere, at Tasken faktisk tilhører Orderen.

Eksempel:

```text
Employee
ManagerId = Mads

Order
ManagerId = Mads

Task
OrderId = Order 100
```

Registreringen er tilladt.

Hvis:

```text
Employee
ManagerId = Anders

Order
ManagerId = Mads
```

afvises registreringen.

---

# Statusflow

TimeEntry-status følger dette flow:

```text
Draft
  ├── Returned -> Draft
  └── Accepted -> Finalized
```

Status betyder:

### Draft

TimeEntry er oprettet og kan behandles.

### Returned

En manager har returneret TimeEntry med en forklaring.

Medarbejderen kan derefter redigere TimeEntry og resubmitte den:

```text
Returned -> Draft
```

### Accepted

Manager har accepteret TimeEntry til fakturering eller eksport.

En Accepted TimeEntry kan ikke længere redigeres af medarbejderen.

### Finalized

TimeEntry er endeligt behandlet.

En Finalized TimeEntry kan ikke ændres.

---

# Redigering af TimeEntry

En medarbejder kan kun redigere sin egen TimeEntry.

TimeEntry kontrollerer:

```text
actorId == EmployeeId
```

Derudover skal status være:

```text
Draft
```

eller:

```text
Returned
```

Følgende egenskaber kan ændres:

```text
Hours
Date
Comment
```

Efter en Return kan medarbejderen rette TimeEntry og kalde:

```text
Resubmit()
```

som ændrer status:

```text
Returned -> Draft
```

---

# TimeEntryReview

`TimeEntryReview` er en Entity inde i `TimeEntry`-aggregatet.

Den bruges til at gemme historikken over managerens behandling af en TimeEntry.

Eksempel:

```text
TimeEntry
 │
 ├── Review: Returned
 │       Reason: "Forkert antal timer"
 │
 ├── Review: Returned
 │       Reason: "Kommentar mangler"
 │
 └── Review: Accepted
```

En review indeholder:

```text
TimeEntryReviewId
Decision
Reason
ReviewedAt
```

Review behøver ikke selv indeholde en `TimeEntryId` i Domain-modellen, når den er en child entity i TimeEntry-aggregatet. Relationens foreign key kan håndteres af Persistence/EF Core.

Mulige beslutninger er:

```text
Returned
Accepted
Finalized
```

`Reason` er påkrævet ved:

```text
Returned
```

---

# Review af TimeEntry

Når en manager reviewer en TimeEntry:

```text
1. Manageren henter relevante TimeEntries.
2. Manageren vælger en Draft-entry.
3. Domain kontrollerer managerens rolle.
4. Domain kontrollerer managerens status.
5. Domain kontrollerer managerens adgang til medarbejderen.
6. Manageren accepterer eller returnerer TimeEntry.
7. TimeEntry-status ændres.
8. En TimeEntryReview oprettes.
```

Ved accept:

```text
Draft -> Accepted
```

Ved returnering:

```text
Draft -> Returned
```

Ved returnering skal en årsag angives.

Medarbejderen kan derefter se årsagen, rette sin TimeEntry og resubmitte den:

```text
Returned -> Draft
```

En manager kan kun finalisere en accepteret TimeEntry:

```text
Accepted -> Finalized
```

---

# Ugevisning og totaler

Der findes ikke længere en `TimeSheet`-metode til ugevisning.

Ugevisning implementeres som en query i Application/Persistence.

Queryen filtrerer på:

```text
EmployeeId
Date >= WeekStart
Date < WeekEndExclusive
```

Den ugentlige total beregnes som:

```text
SUM(TimeEntry.Hours)
```

for de TimeEntries, der matcher medarbejderen og ugeintervallet.

En uge kan derfor gå på tværs af to måneder uden at kræve et nyt Domain-objekt.

Eksempel:

```text
WeekStart:
2026-09-28

WeekEndExclusive:
2026-10-05
```

Der kan dermed være entries fra både september og oktober i samme uge.

---

# Eksport

Eksport er en query/reporting-funktion og er ikke en Domain-entitet.

En autoriseret bruger vælger:

```text
FromDate
ToDateExclusive
```

Application/Persistence henter TimeEntries med:

```text
TimeEntry.Date >= FromDate
TimeEntry.Date < ToDateExclusive
```

Eksporten kan filtrere på:

* Dato
* Medarbejder
* Order
* Task
* Status

Som udgangspunkt eksporteres:

```text
Accepted
Finalized
```

fordi disse entries er godkendt til brug uden for systemet.

En eksport-række kan indeholde:

```text
Employee
Date
Order
Task
Hours
Comment
Status
```

Hvis systemet senere understøtter start- og sluttidspunkt, kan disse også inkluderes i eksporten. De er ikke en del af den nuværende `TimeEntry`-model.

---

# Domain-ansvar og lagdeling

Domain indeholder forretningsreglerne, men ikke HTTP- eller databasekode.

```text
WebAPI
  Modtager HTTP requests og returnerer HTTP responses

Application
  Koordinerer commands og queries

Domain
  Håndhæver forretningsregler,
  aggregate-regler og statusovergange

Persistence
  Henter og gemmer data
```

Et typisk command-flow er:

```text
Frontend
   ↓
WebAPI Request
   ↓
ObjectMapper
   ↓
Application Command
   ↓
ICommandDispatcher
   ↓
Command Handler
   ↓
Domain Aggregate / Domain Service
   ↓
Repository Interface
   ↓
Persistence
   ↓
Database
```

Ugevisning og eksport går gennem queries:

```text
Frontend
   ↓
WebAPI Query
   ↓
Application Query Handler
   ↓
Persistence Query
   ↓
DTO / Export Result
```

---

# Aggregate-struktur

Domain-modellen består af følgende Aggregate Roots:

```text
User
Order
TimeEntry
```

Entities:

```text
Order
 └── Task

TimeEntry
 └── TimeEntryReview
```

Domain Service:

```text
TimeRegistrationDomainService
```

Relationerne kan illustreres således:

```text
                 User
                  │
          ManagerId│
                  │
        ┌─────────┴─────────┐
        │                   │
        ▼                   │
      Order                 │
        │                   │
        │ contains          │
        ▼                   │
      Task                  │
        │                   │
        │ TaskId            │
        ▼                   │
    TimeEntry ◄─────────────┘
        │
        │ contains
        ▼
 TimeEntryReview
```

Den vigtigste adgangsregel er:

```text
Employee.ManagerId == Order.ManagerId
```

og TimeEntry peger direkte på den konkrete Task:

```text
TimeEntry.TaskId -> Task.TaskId
```

Order findes gennem:

```text
Task.OrderId -> Order.Id
```

---

# Aktuelle Domain-klasser

Domain bør efter ændringerne primært indeholde:

```text
Domain
├── Aggregate
│   ├── User.cs
│   ├── Order.cs
│   └── TimeEntry.cs
│
├── Entities
│   ├── Task.cs
│   └── TimeEntryReview.cs
│
├── Services
│   └── TimeRegistrationDomainService.cs
│
├── Interfaces
│   └── ITimeEntryRepository.cs
│
├── ValueObjects
│   ├── UserId.cs
│   ├── OrderId.cs
│   ├── OrderName.cs
│   ├── TaskId.cs
│   ├── TimeEntryId.cs
│   └── TimeEntryReviewId.cs
│
└── Common
    └── IUnitOfWork
```

Der skal ikke længere bruges:

```text
Case
CaseId
WorkCase
WorkId
TimeSheet
TimeSheetId
TimeSheetStatus
Tenant
TenantId
PayrollPeriod
AssignedEmployeeIds
```

---

# Samlet model

Den samlede Domain-model kan derfor beskrives således:

```text
User
 ├── UserId
 ├── ManagerId?
 ├── Name
 ├── Email
 ├── Role
 └── Status


Order
 ├── OrderId
 ├── ManagerId
 ├── Name
 ├── Status
 └── Tasks[]
       │
       └── Task
            ├── TaskId
            ├── OrderId
            ├── Title
            └── Description


TimeEntry
 ├── TimeEntryId
 ├── EmployeeId
 ├── TaskId
 ├── Date
 ├── Hours
 ├── Comment
 ├── Status
 └── Reviews[]
       │
       └── TimeEntryReview
            ├── TimeEntryReviewId
            ├── Decision
            ├── Reason
            └── ReviewedAt
```

Den centrale forretningsregel er, at en medarbejder kun kan registrere tid på en Task, hvis Tasken tilhører en Order, som ligger inden for medarbejderens manager-scope.

`TimeEntry.Create()` opretter selve tidsregistreringen som et Domain-objekt. `TimeRegistrationDomainService` og `Order.CanRegisterTime()` kontrollerer, om registreringen er tilladt. Repository og Unit of Work sørger derefter for persistence.

Dermed er ansvarene adskilt mellem Domain, Application, WebAPI og Persistence, samtidig med at de vigtigste forretningsregler håndhæves i Domain.
