# Forretningsproces for tidsregistrering i OrdreFlow

Dette dokument beskriver hele forretningsprocessen for tidsregistrering i OrdreFlow. Processen går fra oprettelse af sager og arbejdsopgaver til medarbejderens registrering, managerens godkendelse og den endelige låsning af de enkelte tidsregistreringer.

Processen er baseret på følgende princip:

```text
Manager/Admin opretter grunddata
        ->
Medarbejderen registrerer sin egen tid
        ->
Registreringen gemmes straks som Draft
        ->
Manager/Admin kan se registreringen
        ->
Manager/Admin godkender den enkelte TimeEntry
        ->
TimeEntry bliver Approved eller Rejected
        ->
En Approved TimeEntry kan låses
```

En `TimeSheet` er en månedlig beholder for medarbejderens registreringer. Den enkelte `TimeEntry` er den enhed, som medarbejderen opretter, redigerer og får godkendt.

---

## 1. Roller og ansvar

### Medarbejder

Medarbejderen kan:

- Logge ind og se sin egen kalender.
- Se egne registreringer pr. dag og uge.
- Vælge eksisterende sager og Work Items.
- Oprette egne tidsregistreringer.
- Redigere egne `Draft`- og `Rejected`-registreringer.
- Se status på egne registreringer.

Medarbejderen kan ikke:

- Oprette eller lukke en sag.
- Oprette eller fjerne Work Items.
- Vælge en anden tenant.
- Registrere tid på en lukket sag.
- Godkende eller låse egne registreringer.
- Redigere en `Approved` eller `Locked` registrering.

### Manager

Manageren kan:

- Oprette og vedligeholde sager i egen tenant.
- Oprette og vedligeholde Work Items på sager i egen tenant.
- Se medarbejdernes registreringer i egen tenant løbende.
- Godkende eller afvise den enkelte `TimeEntry`.
- Låse godkendte `TimeEntry`-objekter.
- Lukke og genåbne sager.

Manageren skal ikke godkende hver registrering, før medarbejderen kan gemme den. Godkendelse sker efterfølgende.

### Administrator

Administratoren har de samme funktioner som manageren og kan desuden administrere brugere og roller efter de gældende regler.

Administratorens handlinger er altid begrænset til egen tenant.

---

## 2. Tenant og adgangsafgrænsning

OrdreFlow er et multi-tenant-system. Alle brugere, sager, Work Items, TimeSheets og TimeEntries hører til en tenant.

En bruger må kun arbejde med data fra sin egen tenant.

Ved enhver skrivehandling kontrolleres blandt andet:

- Den aktuelle bruger er identificeret af backend.
- Den aktuelle bruger er aktiv.
- Brugerens tenant matcher objektets tenant.
- Medarbejderen kun arbejder på sit eget `TimeSheet`.
- Manageren eller administratoren kun godkender entries i egen tenant.
- Et ID fra et request ikke kan bruges til at hente eller ændre data fra en anden tenant.

`TenantId` skal komme fra den aktuelle bruger eller backendens kontekst. Den må ikke vælges frit i medarbejderens brugergrænseflade.

---

## 3. Manageren opretter en sag

Processen starter med, at en manager eller administrator opretter en sag.

Eksempel:

```text
Sag: Kunde ABC - Produktionsanlaeg
``` 

Ved oprettelsen kontrollerer systemet, at brugeren:

- Er aktiv.
- Har rollen `Manager` eller `Admin`.
- Opretter sagen i sin egen tenant.
- Angiver et gyldigt sagsnavn.

En ny sag får status:

```text
Open
```

En `Open` sag kan indeholde Work Items og kan bruges til nye tidsregistreringer.

Resultatet for manageren er, at sagen kan ses i sagsoversigten. Resultatet for medarbejderen er, at sagen senere kan vælges i tidsregistreringskalenderen, hvis medarbejderen har adgang til den gennem backendens tenant-filtrering.

---

## 4. Manageren opretter Work Items

Manageren eller administratoren opretter de konkrete arbejdsopgaver på sagen.

Eksempel:

```text
Sag: Kunde ABC - Produktionsanlaeg

Work Items:
- Fejlfinding
- Udskift printkort
- Test anlaeg
- Dokumentation
```

Et Work Item har blandt andet:

- `WorkId`
- `CaseId`
- Titel
- Beskrivelse

Et Work Item tilhører præcis én sag. Det er ikke nødvendigvis tildelt én bestemt medarbejder. Flere medarbejdere kan registrere tid på samme Work Item.

Når et Work Item tilføjes, kontrollerer `Case` blandt andet:

- Sagen er åben.
- Work Item har en gyldig titel.
- Beskrivelsen er gyldig.
- Work Item ikke allerede er knyttet til en anden sag.

Hvis sagen er lukket, kan der ikke tilføjes eller fjernes Work Items.

---

## 5. Medarbejderen åbner sin kalender

Når medarbejderen logger ind, åbner systemet medarbejderens kalender eller ugeoversigt.

Systemet finder automatisk det relevante månedlige `TimeSheet` for medarbejderen. Medarbejderen skal ikke selv oprette eller vælge et `TimeSheet`.

Hvis det relevante `TimeSheet` ikke findes, opretter Application-laget det automatisk gennem Domain-reglerne.

Kalenderen viser:

- Den aktuelle uge.
- Mulighed for forrige og næste uge.
- Mulighed for at gå tilbage til den aktuelle uge.
- Medarbejderens egne registreringer pr. dag.
- Dagens total.
- Ugens total.
- Status for hver `TimeEntry`.

Eksempel:

```text
Mine timer - Uge 37

Mandag 07/09
Kunde ABC - Produktionsanlaeg
Udskift printkort                    4,0 timer    Draft
Dagens total                         4,0 timer

Tirsdag 08/09
Ingen registreringer

Ugens total                          4,0 timer
```

Kalenderen bruger backendens data som source of truth. Den lokale brugergrænseflade må ikke være den permanente kilde til registreringerne.

---

## 6. Medarbejderen opretter en TimeEntry

Medarbejderen trykker på `Registrer tid` og udfylder formularen.

Eksempel:

| Felt | Eksempel |
|---|---|
| Sag | Kunde ABC - Produktionsanlaeg |
| Work Item | Udskift printkort |
| Dato | 09-09-2026 |
| Timer | 7,5 |
| Starttid | 08:00 |
| Sluttid | 15:30 |
| Kommentar | Udskiftede og testede printkort |

Medarbejderen vælger først sagen. Derefter viser systemet kun Work Items, der tilhører den valgte sag.

Medarbejderen kan ikke oprette en ny sag eller et nyt Work Item fra formularen.

Hvis virksomheden har brug for interne registreringer, skal manageren oprette en fælles sag, for eksempel:

```text
Sag: Intern tid

Work Items:
- Administration
- Møder
- Kursus
- Kørsel
```

En almindelig registrering kræver derfor stadig en sag og et Work Item.

---

## 7. Validering af en ny registrering

Når medarbejderen trykker `Gem`, sendes requesten til backend. Application-laget henter de relevante aggregates og kalder `TimeRegistrationDomainService`.

Domain Service kontrollerer regler, der går på tværs af flere aggregates:

- `TimeSheet` findes.
- Medarbejderen ejer `TimeSheet`.
- Medarbejderen tilhører samme tenant som `TimeSheet`.
- Medarbejderen er aktiv.
- Sagen tilhører samme tenant som `TimeSheet`.
- Sagen er åben.
- Work Item tilhører den valgte sag.

`TimeEntry` kontrollerer sine egne regler:

- Work Item er påkrævet.
- Dato er påkrævet.
- Timer er større end 0.
- Timer er højst 24 for en enkelt entry.
- Starttid og sluttid er gyldige.
- Hvis begge tider er udfyldt, er sluttid efter starttid.
- Kommentar er højst 1000 tegn.

`TimeSheet` kontrollerer sine egne regler:

- Datoen ligger i samme år og måned som `TimeSheet`.
- Den samlede tid for datoen overstiger ikke 24 timer.
- Entry kan knyttes til det aktuelle `TimeSheet`.

Valideringen sker på backend og i Domain. Brugergrænsefladens validering er kun en hjælp til brugeren og må ikke være den eneste beskyttelse.

---

## 8. Succesfuld gemning

Hvis alle regler er opfyldt, gemmes `TimeEntry` straks i databasen.

Status bliver:

```text
Draft
```

Det er vigtigt, at gemning og godkendelse er to forskellige handlinger:

```text
Gem registrering:
TimeEntry gemmes
TimeEntry.Status = Draft
Manager behøver ikke være til stede
```

Der ændres ikke automatisk status til `Approved`.

Efter gemning henter frontend den valgte uge igen fra backend. Den nye registrering vises derefter i kalenderen.

Eksempel:

```text
Onsdag 09/09
Kunde ABC - Produktionsanlaeg
Udskift printkort                    7,5 timer    Draft
Dagens total                         7,5 timer
Ugens total                          7,5 timer
```

### Resultat for medarbejderen

- Registreringen er gemt.
- Den kan ses i kalenderen.
- Den har status `Draft`.
- Medarbejderen kan fortsætte med flere registreringer.

### Resultat for manageren

- Den nye registrering kan hentes i managerens oversigt.
- Manageren kan se den, selvom den ikke er godkendt.
- Manageren behøver ikke godkende registreringen, før den gemmes.

---

## 9. Managerens løbende oversigt

Manageren eller administratoren kan løbende se medarbejdernes registreringer i egen tenant.

Eksempel:

```text
Medarbejder: Peter Hansen
Dato: 09-09-2026
Sag: Kunde ABC - Produktionsanlaeg
Work Item: Udskift printkort
Timer: 7,5
Status: Draft
```

Managerens oversigt kan filtreres på:

- Medarbejder.
- Dato.
- Uge.
- Sag.
- Work Item.
- Status.

En manager ser registreringen ved næste hentning af data. Automatisk opdatering uden refresh kræver senere polling eller en realtidsmekanisme; det ændrer ikke den underliggende forretningsproces.

Manageren kan se medarbejdernes entries, men kan ikke ændre medarbejderens registrering direkte som en almindelig redigering. Manageren bruger i stedet godkend, afvis eller lås.

---

## 10. Redigering før godkendelse

Medarbejderen kan redigere sin egen `TimeEntry`, når status er:

```text
Draft
```

En afvist entry med status `Rejected` kan også åbnes igen og rettes.

Medarbejderen kan ændre:

- Timer.
- Dato.
- Starttid.
- Sluttid.
- Kommentar.

Alle ændringer går gennem `TimeSheet`, som kontrollerer ejerskab, status, månedsperiode og daglig timegrænse.

### Eksempel på redigering

```text
Før:
7,5 timer, kommentar: Udskiftede printkort

Efter:
8,0 timer, kommentar: Udskiftede og testede printkort
```

Efter en vellykket ændring er status fortsat `Draft`, og kalenderen hentes igen fra backend.

En medarbejder kan ikke redigere:

```text
Approved
Locked
```

En medarbejder fra en anden tenant kan ikke redigere entry’en, selv om personen kender dens ID.

---

## 11. Godkendelse af den enkelte TimeEntry

Manager eller administrator åbner sin oversigt og vælger en konkret registrering.

Godkendelsen gælder kun den valgte `TimeEntry`, ikke hele medarbejderens månedlige `TimeSheet`.

Ved godkendelse kontrolleres:

- Manageren er aktiv.
- Manageren har rollen `Manager` eller `Admin`.
- Manageren tilhører samme tenant.
- Entry’en tilhører det valgte `TimeSheet`.
- Entry’en har status `Draft`.

Ved succes sker statusændringen:

```text
Draft -> Approved
```

### Resultat for manageren

- Registreringen vises som godkendt.
- Den kan ikke længere redigeres af medarbejderen.
- Den kan senere låses.

### Resultat for medarbejderen

- Kalenderen viser status `Approved`.
- Redigeringsknappen fjernes eller deaktiveres.
- Registreringen er stadig synlig som historik.

En medarbejder kan ikke godkende sin egen entry.

---

## 12. Afvisning og rettelse

Hvis en manager finder en fejl, afvises den enkelte entry.

Eksempel på årsag:

```text
Kommentar mangler.
Forkert Work Item.
Forkert antal timer.
Registreringen ligger på en forkert dato.
```

Statusændringen er:

```text
Draft -> Rejected
```

Efter afvisning kan medarbejderen se:

- At entry’en er afvist.
- Eventuel afvisningsforklaring, hvis den er gemt af systemet.
- Hvilke oplysninger der skal rettes.

Medarbejderen retter entry’en og gemmer den igen. Når den er rettet, kan den sendes til ny behandling.

Det tilladte forløb er:

```text
Draft -> Rejected -> Draft -> Approved
```

En afvist entry bliver ikke automatisk godkendt efter redigering. Manageren skal godkende den igen.

---

## 13. Låsning af en godkendt registrering

Når en entry er godkendt, kan manager eller administrator låse den.

Statusændringen er:

```text
Approved -> Locked
```

En låst entry er endelig:

- Medarbejderen kan ikke redigere den.
- Manageren kan ikke ændre den gennem almindelig redigering.
- Den kan bruges som afsluttet historik og rapporteringsgrundlag.

Det samlede statusflow er:

```text
Draft -----------------> Approved -----------------> Locked
  |
  +---------------------> Rejected -----------------> Draft
```

---

## 14. Lukning af en sag

Når arbejdet på sagen er afsluttet, kan Manager eller Admin lukke sagen.

Statusændringen er:

```text
Open -> Closed
```

Efter lukning:

- Nye Work Items kan ikke oprettes.
- Work Items kan ikke fjernes.
- Sagen kan ikke omdøbes.
- Nye tidsregistreringer på sagen afvises.
- Eksisterende TimeEntries bevares som historik.

En lukket sag kan genåbnes af Manager eller Admin:

```text
Closed -> Open
```

Når sagen er genåbnet, kan nye registreringer igen oprettes, hvis de øvrige regler er opfyldt.

---

## 15. Alle fejlscenarier for medarbejderen

| Situation | Resultat |
|---|---|
| Ingen sag valgt | Registreringen gemmes ikke. Brugeren skal vælge en sag. |
| Intet Work Item valgt | Registreringen gemmes ikke. Brugeren skal vælge et Work Item. |
| Work Item tilhører en anden sag | Registreringen afvises. |
| Sagen er lukket | Registreringen afvises. |
| Sagen tilhører en anden tenant | Registreringen afvises uden at lække data. |
| Medarbejderen er inaktiv | Registreringen og ændringen afvises. |
| Timer er 0 eller negative | Registreringen afvises. |
| En enkelt entry er over 24 timer | Registreringen afvises. |
| Dagens samlede timer er over 24 | Registreringen afvises. |
| Dato ligger i en anden måned | Registreringen afvises af `TimeSheet`. |
| Sluttid er før eller lig med starttid | Registreringen afvises. |
| Kommentar er over 1000 tegn | Registreringen afvises. |
| Entry er Approved | Medarbejderen kan ikke redigere den. |
| Entry er Locked | Medarbejderen kan ikke redigere den. |
| Medarbejderen forsøger at bruge et fremmed ID | Backend afviser handlingen gennem tenant- og ejerskabskontrol. |

---

## 16. Alle fejlscenarier for manageren

| Situation | Resultat |
|---|---|
| Manager forsøger at se en anden tenant | Ingen adgang til data. |
| Employee forsøger at godkende | Handlingen afvises. |
| Inaktiv manager forsøger at godkende | Handlingen afvises. |
| Manager forsøger at godkende en allerede godkendt entry | Handlingen afvises. |
| Manager forsøger at låse en Draft entry | Handlingen afvises. |
| Manager forsøger at låse en Rejected entry | Handlingen afvises. |
| Manager forsøger at godkende en entry fra en anden tenant | Handlingen afvises. |
| Manager forsøger at godkende en entry, der ikke tilhører timesheet | Handlingen afvises. |
| Manager lukker en sag, der allerede er lukket | Handlingen afvises. |
| Employee forsøger at lukke en sag | Handlingen afvises. |

---

## 17. Samlet eksempel fra start til slut

### Forudsætninger

```text
Tenant: VS Automatic
Manager: Mads
Medarbejder: Peter
```

### Grunddata

Mads opretter:

```text
Sag: Kunde ABC - Produktionsanlaeg
Work Item: Udskift printkort
```

### Medarbejderens registrering

Peter logger ind og opretter:

```text
Dato: 09-09-2026
Sag: Kunde ABC - Produktionsanlaeg
Work Item: Udskift printkort
Timer: 7,5
Starttid: 08:00
Sluttid: 15:30
Kommentar: Udskiftede og testede printkort
```

Systemet validerer registreringen og gemmer:

```text
TimeEntry.Status = Draft
```

Peter kan straks se registreringen i sin kalender. Mads kan straks se registreringen i manageroversigten.

### Managerens behandling

Mads ser, at registreringen er korrekt, og godkender den:

```text
Draft -> Approved
```

Peter kan nu se registreringen som `Approved`, men kan ikke længere ændre den.

Når registreringen er endeligt behandlet, låser Mads den:

```text
Approved -> Locked
```

### Afslutning

Når arbejdet på sagen er færdigt, lukker Mads sagen:

```text
Open -> Closed
```

Den eksisterende registrering bliver bevaret som historik, men nye registreringer på sagen er ikke længere tilladt.

---

## 18. Slutresultat for brugerne

### Medarbejderen ser

```text
Min kalender

09-09-2026
Kunde ABC - Produktionsanlaeg
Udskift printkort
7,5 timer
Status: Locked

Ugens total: 7,5 timer
```

Medarbejderen ved:

- Hvilken sag tiden er registreret på.
- Hvilket Work Item tiden er registreret på.
- Hvilken dag og uge tiden tilhører.
- Om registreringen stadig kan rettes.
- Om den er godkendt eller afsluttet.

### Manageren ser

```text
Medarbejder: Peter Hansen
Sag: Kunde ABC - Produktionsanlaeg
Work Item: Udskift printkort
Dato: 09-09-2026
Timer: 7,5
Status: Locked
```

Manageren ved:

- Hvem der har registreret tiden.
- Hvilken sag og opgave tiden vedrører.
- Om registreringen er Draft, Approved, Rejected eller Locked.
- Hvilke entries der mangler behandling.
- Hvilke registreringer der er endeligt låst.

### Virksomheden ser

Virksomheden får:

- Sporbare tidsregistreringer.
- Tenant-isolerede data.
- Godkendelse på den enkelte registrering.
- Historik over godkendte og låste entries.
- Mulighed for daglige, ugentlige og månedlige summer.
- En proces, hvor medarbejderen ikke er afhængig af manageren for at gemme sin tid.

---

## 19. Teknisk ansvar i arkitekturen

Forretningsprocessen gennemføres gennem den eksisterende arkitektur:

```text
Blazor / klient
        ->
API Request
        ->
ObjectMapper
        ->
Command
        ->
ICommandDispatcher
        ->
ICommandHandler
        ->
Repository og Domain Service
        ->
Aggregate Root
        ->
Unit of Work / Persistence
        ->
Database
```

Ansvarsfordelingen er:

- Endpoint håndterer HTTP.
- Mapper håndterer Request/Command/Response.
- Handler koordinerer use casen.
- Repository henter og gemmer data.
- `TimeRegistrationDomainService` koordinerer tværgående regler.
- `Case` beskytter regler for sag og Work Items.
- `TimeSheet` beskytter regler for måned, dag og entries.
- `TimeEntry` beskytter egne værdier og statusovergange.
- Database gemmer det godkendte resultat.

Domænet må ikke erstattes af frontend-validering. Frontend må gerne vise hurtige fejlbeskeder, men backend og Domain skal altid kontrollere reglerne igen.
