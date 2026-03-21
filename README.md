# Cinema Mob

## Vilka har arbetat i projektet?

Git-användare:

- EbadianH - Havash Ebadian
- Mezea11 - Christian Meza
- LukWen-Ill - Lukas Wennström
- mannilowman - Emmanuel Lowman
- Mo - Mohamed Adam

---

## Kortfattad projektbeskrivning

Cinema Mob är en webbapplikation för en biograf med gangster-tema. Användaren kan se filmer och visningar, läsa filminformation, registrera konto, logga in, boka platser och få bokningsbekräftelse.

Projektet innehåller även ett adminläge för filmhantering och kontaktmeddelanden samt en AI-chatt för frågor och svar kring bio, visningar och bokning.

---

## Installation, inkl. databashantering

### Krav

- Node.js och npm
- .NET SDK 10.0
- MySQL-klient, till exempel DBeaver eller MySQL Workbench

### Start av projektet

1. Kör `npm install` i projektroten.
2. Kontrollera att databasanslutningen i `backend/db-config.json` pekar mot rätt MySQL-databas.
3. Initiera databasen med SQL-filerna i `backend/migrations/`.
4. Starta projektet med `npm run dev`.

### Databas

1. Kör `001_schema.sql` för att skapa tabeller.
2. Kör `002_views_and_procedures.sql` för vyer och lagrade procedurer.
3. Kör `003_seed_data.sql` endast om ni vill lägga in eller återställa exempeldata.

---

## Viktigt att veta

- Frontend är byggd i React/Vite och backend är en separat .NET Minimal API-lösning.
- `npm run dev` startar frontend och startar/proxar backend via `backend/index.js`.
- Projektet använder sessionshantering och ACL-regler i backend för inloggning och behörighet.
- Platsbokning använder seat locking och strömmad status för att minska dubbelbokningar.
- `003_seed_data.sql` tömmer flera tabeller innan den fyller på data. Den ska därför inte köras mot en delad databas utan att gruppen är överens.
- Känsliga uppgifter som databas-, e-post- och AI-inställningar ligger i `backend/db-config.json` och bör egentligen flyttas till miljövariabler.

---

## Teknisk skuld

- Konfiguration med hemligheter ligger i versionshanterad fil istället för i miljövariabler.
- Databasinstallation och migreringar är manuella och delvis beroende av att man kör rätt SQL-filer i rätt ordning.
- Seed-scriptet är destruktivt och mindre lämpligt i en delad molndatabas.
- Projektet har begränsad automatiserad testning, vilket gör regressionsrisk högre vid vidareutveckling.
- Frontend och backend startas via en speciallösning där Node startar .NET-processen, vilket fungerar i utveckling men är mindre tydligt för deployment.

---

## Lösningsarkitektur

- **Frontend:** React 19, TypeScript, Vite och React Router
- **Backend:** C# .NET Minimal API med route-baserad logik för bland annat inloggning, bokningar, admin och AI-chatt
- **Databas:** MySQL med schema, vyer, procedurer och seed-data via SQL-migreringar
- **Integration:** Frontend anropar backend via `/api`, och utvecklingsservern proxar trafiken vidare
- **Realtid:** Platsstatus uppdateras löpande för bokningsflödet

---

## Planerat men ej genomfört arbete

- Bredare automatiserad testning för frontend, backend och bokningsflöde
- Säkrare hantering av hemligheter och tydligare deploy/process för produktion
- Mer komplett adminstöd, till exempel fler verktyg för drift, uppföljning och innehållshantering
- Vidareutveckling av AI-chatten med bättre felhantering, observability och tydligare begränsningar
