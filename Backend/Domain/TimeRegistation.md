# Forretningsproces for tidsregistrering

Denne forretningsproces beskriver, hvordan tidsregistrering foregår i OrdreFlow – fra en Manager eller Administrator opretter en sag, til medarbejderen registrerer sin tid, og registreringen efterfølgende bliver godkendt og låst.

---

## 1. Oprettelse af sag

Processen starter med, at en **Manager eller Administrator** opretter en sag i systemet.

Et eksempel kunne være:

> **Sag:** Kunde ABC – Produktionsanlæg

Sagen bliver automatisk knyttet til den **tenant**, som brugeren tilhører. Det betyder, at sagen kun kan tilgås af brugere fra den samme virksomhed.

En ny sag får status:

`Open`

Statussen `Open` betyder, at der stadig arbejdes på sagen, og at medarbejdere kan registrere tid på den.

---

## 2. Oprettelse af Work Items

Når sagen er oprettet, kan Manageren eller Administratoren oprette de konkrete arbejdsopgaver, som hører til sagen.

Eksempel:

- Fejlfinding
- Udskiftning af printkort
- Test af anlæg
- Dokumentation

Disse arbejdsopgaver kaldes **Work Items**.

Et Work Item tilhører altid en bestemt sag, men bliver ikke nødvendigvis tildelt en bestemt medarbejder.

Medarbejderen kan derfor selv vælge den relevante arbejdsopgave, når der skal registreres tid.

---

## 3. Medarbejderen logger ind

Når medarbejderen skal registrere sin arbejdstid, logger medarbejderen ind i systemet.

Medarbejderen bliver præsenteret for en kalender eller ugeoversigt over sine egne registreringer.

Oversigten viser blandt andet:

- Den aktuelle uge
- Registreringer pr. dag
- Dagens samlede timer
- Ugens samlede timer
- Status på de enkelte registreringer

Formålet er at give medarbejderen et hurtigt overblik over den registrerede arbejdstid.

---

## 4. Medarbejderen opretter en tidsregistrering

Når medarbejderen skal registrere sin arbejdstid, vælger medarbejderen først den relevante sag og derefter det konkrete Work Item.

Et eksempel kunne være:

| Felt | Værdi |
|---|---|
| Sag | Kunde ABC – Produktionsanlæg |
| Work Item | Udskift printkort |
| Dato | 09-09-2026 |
| Timer | 7,5 |
| Starttid | 08:00 |
| Sluttid | 15:30 |
| Kommentar | Udskiftede printkort |

Medarbejderen sender herefter registreringen til systemet.

Medarbejderen kan kun vælge mellem de sager og Work Items, som allerede er oprettet i systemet.

---

## 5. Systemet validerer registreringen

Inden registreringen gemmes, bliver den valideret.

Systemet kontrollerer blandt andet:

- Medarbejderen er aktiv.
- Medarbejderen tilhører samme tenant som sagen.
- Medarbejderen registrerer på sit eget timesheet.
- Sagen tilhører samme tenant som medarbejderen.
- Sagen er åben.
- Work Item tilhører den valgte sag.
- Datoen ligger i den relevante timesheet-periode.
- Antallet af timer er større end 0.
- En enkelt registrering ikke overstiger 24 timer.
- Medarbejderens samlede timer for dagen ikke overstiger 24 timer.
- Starttid og sluttid er gyldige.
- Kommentaren ikke overstiger 1000 tegn.

Denne validering sikrer, at ugyldige data ikke bliver gemt i systemet.

Valideringen sker i domænelaget, så forretningsreglerne ikke kun afhænger af brugergrænsefladen.

---

## 6. Registreringen gemmes

Hvis alle valideringer er godkendt, bliver `TimeEntry` gemt i databasen.

Registreringen får status:

`Draft`

Det betyder, at registreringen er oprettet, men endnu ikke er godkendt.

Det er vigtigt, at medarbejderen ikke skal vente på en Manager, hver gang der registreres tid.

Medarbejderen kan derfor fortsætte med at registrere tid på andre dage og arbejdsopgaver.

Eksempel:

```text
Mandag    → 7,5 timer → Draft
Tirsdag   → 8,0 timer → Draft
Onsdag    → 6,0 timer → Draft
Torsdag   → 7,5 timer → Draft
Fredag    → 8,0 timer → Draft