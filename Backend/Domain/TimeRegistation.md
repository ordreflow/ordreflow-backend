# Time Registration Domain Model

Dette dokument beskriver den aktuelle M3-model i OrdreFlow: aggregate-grænser,
adgangsregler, tidsregistreringens livscyklus og fordelingen af ansvar mellem
Domain og de øvrige lag.

Hver virksomhed har sit eget miljø. Domain indeholder derfor ikke `Tenant`,
`TenantId` eller virksomhedsskift. De tekniske navne `Catagory` og `Catagories`
følger stavemåden i den nuværende kode.

## Aggregate-struktur

```text
Department                         Employee
  └── Catagories                     └── Accesses
        └── Tasks                          └── EmployeeSystemAccess

TimeEntry                          Export
  ├── EmployeeId                     └── TimeEntries
  ├── Audits                               └── TimeEntryExport
  │     └── TimeEntryAudit                        ├── TimeEntryId
  └── Reviews                                    └── TimeEntryAuditId
        └── TimeEntryReview

RegistrationPeriodMode             AccessPrivilege (under udbygning)
                                     └── Tasks
                                           └── AccessTask
```

`Department`, `Employee`, `TimeEntry`, `Export` og `RegistrationPeriodMode`
ligger i `Domain.Aggregate`. `AccessPrivilege` ligger også her, men dens
forretningsmetoder og integration i skriveoperationerne er endnu ikke færdige.

`Catagory` og `Task` er child entities i **Department-aggregatet** og ligger i
`Domain.Entities`. En kategori er ikke et selvstændigt aggregate root.

### Relationer på tværs af aggregates

Følgende link entities forbinder identiteter uden at gøre Employee til en del af
Department-aggregatet:

| Link entity | Referencer og betydning |
| --- | --- |
| `DepartmentEmployee` | `DepartmentId` og `EmployeeId`: medlemskab af en afdeling |
| `DepartmentManager` | `DepartmentId` og `ManagerId`: ledelsesansvar for en afdeling |
| `CatagoryReviewer` | `CatagoryId` og `EmployeeId`: ansvar for review i en kategori |
| `TaskAssignment` | `TaskId` og `AssignedTo`: tildeling af arbejde til en medarbejder |

En medarbejder kan tilhøre flere afdelinger, lede flere afdelinger og reviewe flere
kategorier. Disse relationer er uafhængige af hinanden.

`TaskAssignment` indeholder desuden `ExpectedDuration` og `ExpectedDeadline`
for den enkelte medarbejders tildeling.

## Indkapsling og ansvar

Aggregate roots beskytter deres egen tilstand og deres child entities:

- Department opretter og ændrer sine kategorier og tasks.
- Employee ændrer profil, aktiv status og egne systemadgangsrækker gennem metoder.
- TimeEntry styrer statusovergange, audit-versioner og review-historik.
- Export beskytter sit faste udvalg, filindhold og oplysninger om eksplicit lukning.

Konstruktører og setters er private på disse roots og deres indkapslede children.
En read-only collection er ikke tilstrækkelig alene; dens elementer skal også
beskyttes mod direkte ændring.

Referencer mellem aggregates gemmes som typed IDs. Det forhindrer ikke, at en
domain service modtager flere indlæste aggregates som parametre for at evaluere
en fælles forretningsregel.

Metoder, der kan afvises efter validering eller forretningsregler, returnerer
`Result` eller `Result<T>` med en fejlkode og besked. Domain vælger ikke HTTP-statuskoder.

## Employee og systemadgang

Employee indeholder identitet, navn, email, aktiv status, en valgfri vist titel og
en collection af `EmployeeSystemAccess`.

`Employee.Create` opretter en aktiv medarbejder med grundlæggende
`SystemPermission.Employee`. Yderligere grants gives gennem `GrantPermission`
og fjernes gennem `RevokePermission`. Adgangsrækkerne kan ikke ændres direkte.
`ChangeProfile`, `Deactivate` og `Activate` håndterer de øvrige aktuelle ændringer.

`HasPermission` kræver både aktiv status og den konkrete grant. En vist titel
som "Manager" giver ingen rettigheder.

| Systemadgang | Betydning |
| --- | --- |
| `Employee` | Grundlæggende adgang til medarbejderfunktioner |
| `DepartmentManager` | Systemadgang til ledelsesfunktioner; afdelingernes scope bestemmes af `DepartmentManager`-links |
| `CatagoryReviewer` | Systemadgang til review; kategoriernes scope bestemmes af `CatagoryReviewer`-links |
| `Payroll` | Adgang til eksport og eksplicit lukning af en eksportbatchs registreringer |
| `Administrator` | Administrationsadgang samt adgang til eksport og virksomhedsdækkende rapportering |

Review, eksport og rapportering kræver også den grundlæggende Employee-grant.
Et ledelsesansvar giver ikke automatisk review-adgang, og Administrator-granten
erstatter ikke reviewer-grant og kategoritildeling ved review.

## Department, Catagory og Task

Department er den samlede grænse for kategorier og deres konkrete arbejde:

```text
Department.Id
    └── Catagory.DepartmentId
          └── Task.CatagoryId
```

`Department.Create` opretter rootens identitet. `AddCatagory` og `AddTask`
opretter child-identiteter og parent-referencer inde i aggregatet. Eksternt
oprettede, mutable child-objekter tilføjes ikke direkte til collections.

De aktuelle styringsmetoder omfatter:

```text
Department.AddCatagory(...)
Department.AddTask(catagoryId, ...)
Department.RenameCatagory(catagoryId, ...)
Department.CloseCatagory(catagoryId, ...)
Department.ReopenCatagory(catagoryId)
Department.CloseTask(catagoryId, taskId, ...)
```

Department finder barnet og delegerer den lokale ændring til det. Catagory og
Task har private parent-ID setters, så arbejdet ikke flyttes til en anden
kategori eller omklassificeres til en anden afdeling gennem almindelige setters.

Kategorier og tasks bruger status `Active` og `Closed`. En lukket kategori kan
ikke få nye tasks, og der kan ikke registreres ny tid på en lukket kategori eller
task. Lukning af arbejde og behandling af tidsregistreringer er forskellige processer.

### Adgang til at registrere tid

Den aktuelle adgangsregel kombinerer:

1. Medarbejderen er aktiv og har Employee-granten.
2. Tasken findes under den indlæste Departments kategori.
3. Kategorien og tasken er aktive.
4. En kategori med `InternalOnly` kræver medlemskab af dens afdeling.
5. Hvis tasken har tildelinger, skal medarbejderen være blandt de tildelte.
6. Arbejdsdatoen ligger i den tilladte registreringsperiode.

En task uden tildelinger er generelt valgbar inden for kategoriens adgangsregler.
Medarbejderen behøver ikke at være reviewer for at registrere tid.

`Department.CanRegisterTime(taskId)` kontrollerer sit work item og delegerer til
kategoriens interne `CanRegisterTime(taskId)`. Regler om Employee, medlemskab og
tildeling samles i `TimeRegistrationDomainService`.

Metoder, der søger i child collections, kræver, at de relevante børn er indlæst.
Registreringsrepositoryet indlæser den ejende Department med kategorier og tasks;
en konfigureret database-relation indlæser ikke automatisk sin navigation.

## Value objects, datoer og varighed

IDs og validerede værdier er value objects, blandt andet:

- `EmployeeId`, `DepartmentId`, `CatagoryId`, `TaskId` og `TimeEntryId`.
- `TimeEntryAuditId`, `TimeEntryReviewId`, `ExportId` og `RegistrationPeriodModeId`.
- `PersonName`, `EmailAddress`, `DepartmentName`, `CatagoryName` og `TaskName`.
- `WorkDate`, `Duration`, `TimeEntryDescription`, `RejectionReason` og `WorkDatePeriod`.
- `PlannedDuration`, `TargetCompletionDate` og `CutoffDay`.

`WorkDate` og deadlines er date-only værdier. `Duration` bruger **decimal timer**,
skal være større end 0 og højst 24 pr. registrering og accepterer kvarterstimer
samt de særlige værdier 7,4 og 3,7 timer. Dette er ikke en kontrol af medarbejderens
samlede dagstotal.

Beskrivelser kan være tomme og har højst 1.000 tegn. En afvisningsårsag skal være
udfyldt og har højst 1.000 tegn. Audit- og review-tidspunkter skal være UTC.

## Registreringsperioder

`RegistrationPeriodMode` beskriver virksomhedens tilladte arbejdsdatointerval.
Application bestemmer den aktuelle dato i virksomhedens tidszone og sender den
til `GetPermittedWorkDates(today)`.
Uden en konfigureret periode afvises de aktuelle oprettelses-, redigerings-,
slette- og genindsendelsesflows.

| Interval i koden | Tilladte arbejdsdatoer |
| --- | --- |
| `Daily` | Den aktuelle lokale dato |
| `Weekly` | Mandag til søndag i den aktuelle uge |
| `Monthy` | Den aktuelle kalendermåned |
| `DayInMonth` | Månedlig periode med valgt cutoff-dag fra 1 til 28 |

Ved cutoff den 18. løber perioden fra den 19. i den foregående måned til og med
den 18. En ny periode begynder dagen efter cutoff. `CutoffDay` er påkrævet for
`DayInMonth` og må ikke angives for de andre intervaller.

`Record`, `Change`, `Delete` og `Resubmit` kontrollerer det tilladte interval.
Ved redigering skal både den eksisterende og den nye arbejdsdato være tilladt;
en låst registrering kan ikke flyttes ind i en åben periode for at omgå kontrollen.

`AccessPrivilege` og `AccessTask` er et grundlag for administratorudstedte
undtagelser med medarbejder, udløb og task-scope. Undtagelseskontrollen er endnu
ikke koblet til de aktuelle skriveflows. Den må ikke låse Approved eller Closed op.

## TimeEntry og versionshistorik

TimeEntry er et selvstændigt aggregate root med:

```text
Id: TimeEntryId
EmployeeId: EmployeeId
Revision
Audits: TimeEntryAudit[]
Reviews: TimeEntryReview[]
```

Arbejdsdata og status ligger på audit-versionerne frem for som parallelle,
mutable felter på rooten:

```text
TimeEntryAudit
  Id
  TimeEntryId
  ActorId
  ClosedBy?
  TaskId
  WorkDate
  Duration
  Description
  Status
  AuditTime
```

`EmployeeId` angiver ejeren af registreringen; `ActorId` angiver, hvem der udførte
den konkrete handling. Review og lukning ændrer ikke ejeren.

`GetCurrentAudit` vælger versionen med den største `AuditTime`. `CurrentStatus`,
`CurrentTaskId`, `CurrentWorkDate` og `CurrentDuration` er afledte værdier.
Kategori og omkostningsbærende afdeling findes gennem den aktuelle task.

Hver ny audit skal tilhøre registreringen og have et senere tidspunkt end den
foregående version. `Revision` øges ved hver audit og bruges til optimistisk
konkurrenskontrol i Persistence.

`TimeEntry.Record` er den offentlige oprettelsesmetode. Den modtager validerede
værdier og et tilladt arbejdsdatointerval og opretter den første audit.
Den generiske initial-audit-factory og `AddAudit` er private: tilstandsovergange
skal ske gennem forretningsmetoderne.

### Statusflow

Den aktuelle oprettelsesvej går direkte til `ReadyForApproval`. `Draft` findes
fortsat til eksisterende data og det senere draft/submission-flow; `Submit`
kan sende en Draft videre til ReadyForApproval.

```text
Record ----------------------------------------> ReadyForApproval
Draft -- Submit eller vellykket redigering ----> ReadyForApproval
ReadyForApproval -- vellykket redigering ------> ReadyForApproval
ReadyForApproval -- Reject --------------------> Rejected
Rejected -- Change eller Resubmit -------------> ReadyForApproval
ReadyForApproval -- Approve -------------------> Approved
Approved -- eksplicit lukning via gemt eksport -> Closed

Draft / ReadyForApproval / Rejected -- Delete -> Deleted
```

| Aktuel status | Ejeren kan redigere | Ejeren kan slette |
| --- | --- | --- |
| `Draft` | Ja, inden for perioden | Ja, inden for perioden |
| `ReadyForApproval` | Ja, inden for perioden | Ja, inden for perioden |
| `Rejected` | Ja, inden for perioden | Ja, inden for perioden |
| `Approved` | Nej | Nej |
| `Closed` | Nej | Nej |
| `Deleted` | Nej | Allerede slettet |

Enhver vellykket redigering sætter status til ReadyForApproval. `Change` ændrer
arbejdsdato, varighed og beskrivelse; den fastholder den aktuelle TaskId.
`Resubmit` kan sende en uændret Rejected tilbage til review uden redigering.

Sletning tilføjer en audit med Deleted-status og bevarer historikken. Den aktuelle
afvisningsårsag vises kun, mens status er Rejected. Efter redigering eller
genindsendelse ligger den tidligere årsag fortsat i review-historikken.

## Review

`TimeEntryReview` er en child entity under TimeEntry og indeholder `Id`,
`TimeEntryId`, `ReviewerId`, `Status`, `Description` og `ReviewedAtUtc`.
Status er `Approved` eller `Rejected`; `Description` indeholder en valideret
`RejectionReason` ved afvisning.

Review kræver en aktiv Employee med reviewer-grant og en reviewer-tildeling til
den aktuelle tasks kategori. Medarbejderens hjemafdeling eller manager giver
ikke i sig selv denne adgang. En særskilt `TimeEntryReviewPolicy` bestemmer,
om egne registreringer må reviewes.

Kun ReadyForApproval kan godkendes eller afvises. En afvisning kræver en årsag.
En vellykket beslutning opretter både en ny audit og en review-række. En afvist
operation tilføjer ikke historik.

## Gemte eksporter og eksplicit lukning

Export er et aggregate root, ikke blot en query. En eksport oprettes for et
inkluderende arbejdsdatointerval og enten alle medarbejdere eller en navngiven
medarbejder. Kun registreringer, hvis aktuelle audit er Approved, kan vælges.

Export gemmer blandt andet:

- Identitet, opretter og UTC-oprettelsestidspunkt.
- Dato- og medarbejderfilter samt samlet antal timer.
- Et fast udvalg af `TimeEntryExport`-rækker med `TimeEntryId` og den konkrete
  godkendte `TimeEntryAuditId`.
- Det oprindelige CSV-indhold samt revision og oplysninger om eksplicit lukning.

CSV formateres uden for Domain. Det gemte indhold udleveres som en kopi, så
senere downloads kan returnere den oprindelige eksport trods navneændringer,
nyere registreringer eller ændrede aktuelle statusser.

**Oprettelse og download af en eksport ændrer ikke registreringernes status.**
Payroll eller en administrator vælger en tidligere batch og udfører en særskilt
handling: **Close time entries**.

`ExportClosureDomainService` bruger batchens gemte udvalg, ikke et nyt datofilter.
Den validerer hele udvalget, før den beder hver TimeEntry om at lukke sin godkendte
version og derefter registrerer lukkehandlingen på Export.

Overlappende batches er tilladt. Er den samme godkendte version allerede Closed
via en anden batch, springes den over uden ny lukkeaudit. De resterende Approved
lukkes. Gentagelse af en allerede udført batchhandling ændrer ikke historikken.
En manglende eller ændret version afviser hele handlingen.

TimeEntry bevarer den første faktiske lukkeaktør og det første lukketidspunkt.
Hver batch registrerer separat sin egen lukkehandling. Application og Persistence
sørger for, at batchmetadata og alle entry-ændringer gemmes atomisk.

Application håndterer også genforsøg på eksportoprettelse med samme eksport-ID:
samme opretter og filtre genbruger den gemte batch, mens et andet request med samme
ID afvises som konflikt. Et nyt, bevidst overlappende udvalg bruger et nyt ID.

## Domain services og lagdeling

| Domain service | Samlet forretningsansvar |
| --- | --- |
| `TimeRegistrationDomainService` | Kombinerer Employee-berettigelse, Departments work item, medlemskab/tildelinger og tilladt periode; kalder `TimeEntry.Record` |
| `TimeEntryReviewDomainService` | Kombinerer reviewerens rettigheder, kategoritildeling og review-policy; kalder TimeEntrys godkendelse eller afvisning |
| `ExportClosureDomainService` | Kontrollerer det faste batchudvalg og koordinerer lukning mellem Export og TimeEntry-aggregates |

Services indlæser eller gemmer ikke data. Application leverer indlæste aggregates,
backend-opslåede adgangsfakta og operationstidspunkter. Aggregates håndhæver deres
egne regler og udfører deres egne tilstandsændringer.

```text
WebApi
  Request/response, autentificeret aktør og HTTP-mapping
    ↓
Application handler
  Indlæs data, fastlæg virksomhedslokal dato/UTC-tid, koordiner operationen
    ↓
Domain service, når reglen går på tværs af aggregates
    ↓
Aggregate-metoder og value objects
    ↓
Repositories stage'r ændringer
    ↓
Unit of Work / eksplicit transaktionsgrænse gemmer én gang
```

En domain service er ikke nødvendig for enhver operation. Ejerskab, redigering,
sletning og statuslåsning af én TimeEntry håndteres på selve TimeEntry.
Databaseisolation, rollback og genforsøg med genindlæst tilstand ligger i Persistence
bag en transaktionskontrakt i Application.

## Read-modeller

Ugevisning, reviewer-kø og rapportering er queries, som kan læse direkte fra de
relevante tabeller og returnere DTOs. De behøver ikke indlæse hele Department-
aggregatet eller udføre domain-mutationer.

- Ugevisning og ugetotaler bruger ejerens egne registreringer og den aktuelle audit
  i et mandag-søndag-interval. Deleted udelades. En uge kan krydse en månedsgrænse.
- Reviewer-køen bruger den aktuelle audit, ReadyForApproval og aktuelle
  kategoritildelinger. En ældre autoriseret version må ikke give adgang til en
  nyere version på en anden task.
- Top-kategorirapporten summerer aktuelle, ikke-slettede audits for valgte
  inkluderende arbejdsdatoer. Den viser højst ti kategorier med deres task-totaler.
  Draft, ReadyForApproval, Rejected, Approved og Closed tæller med.
- Rapportens manager-scope er kategorier ejet af afdelinger, vedkommende aktuelt
  leder. Administratorer har virksomhedsdækkende rapportadgang. Medarbejderens
  afdeling bestemmer ikke, hvilken kategori der belastes.

Filtrering på seneste version, ejerskab og scope skal ske korrekt før summering
og rangering. Queries giver ikke automatisk adgang til individuelle medarbejderes
kommentarer eller review-årsager gennem aggregaterede rapporter.

## Implementeringsgrænser

Modellen beskriver domæneregler og ansvar, ikke en garanti for, at hele M3 kan
køres endnu. Ældre Application-flows og den samlede databasemigration er fortsat
under omstilling. AccessPrivilege-undtagelser kræver yderligere implementering.

Normale tests opbygger tilstand gennem factories og forretningsmetoder.
Materialisering af historiske eller fejlbehæftede versionsforløb sker i særskilte
testfixtures. Domain udstiller ikke en offentlig adgang til vilkårlige audits for
at understøtte tests.
