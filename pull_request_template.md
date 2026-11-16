<!--
  MediSharp – Pull Request template (gebaseerd op de Definition of Done v1.0, 10-11-2026)
  Vink elk criterium af ([x]) zodra het is voldaan en geverifieerd.
  Is een criterium niet van toepassing? Vink het af en zet er "N.v.t. – <reden>" achter.
  Een user story is pas Done als ALLE criteria zijn afgevinkt.
-->

## 📋 Gegevens user story

| Onderdeel                        | Gegevens                                  |
| -------------------------------- | ----------------------------------------- |
| **User Story ID**                | <!-- bijv. HUI-1 -->                      |
| **Titel**                        |                                           |
| **Sprint**                       |                                           |
| **Branchnaam**                   | <!-- bijv. Feature/HUI-1-verwijsbrief --> |
| **Jira/Trello-ticket**           | <!-- link -->                             |
| **Reviewers (min. 2 teamleden)** | <!-- @naam1, @naam2 -->                   |

## 📝 Beschrijving

<!-- Wat is er veranderd en waarom? Benoem de belangrijkste technische keuzes. -->

### Type wijziging

- [ ] ✨ Nieuwe feature
- [ ] 🐛 Bugfix
- [ ] ♻️ Refactor
- [ ] 🗄️ Databasewijziging / migratie
- [ ] 📚 Documentatie
- [ ] ⚙️ Configuratie / CI/CD

### Persoonsgegevens (AVG)

<!-- Welke persoonsgegevens worden verwerkt, waarom, en wat is de bewaartermijn? Anders: "Geen". -->

---

## ✅ Definition of Done

### 1. Functionele voltooiing

- [ ] **1.** De user story is volledig geïmplementeerd: alle subtaken zijn afgerond.
- [ ] **2.** Alle acceptatiecriteria zijn behaald, volledig geïmplementeerd en gevalideerd.
- [ ] **3.** Edge cases zijn afgevangen (foutieve invoer, ontbrekende permissies, verlopen sessies).

### 2. Kwaliteit & technische standaarden

- [ ] **4.** Code wordt samengevoegd (gemerged) via deze pull request.
- [ ] **5.** De pull request is beoordeeld en goedgekeurd door minimaal twee teamleden.
- [ ] **6.** Er staan geen onopgeloste opmerkingen meer open.
- [ ] **7.** De implementatie volgt de afgesproken coderingsstandaarden en de stijlgids.
- [ ] **8.** Code voldoet aan de code style guidelines (linting, naming conventions).
- [ ] **9.** Geen ernstige of high-severity issues in linting/scans (SAST/DAST).
- [ ] **10.** De code is gedocumenteerd (methoden, endpoints, ingewikkelde logica).
- [ ] **11.** Databasewijzigingen zijn gemigreerd en getest.

### 3. Testen

- [ ] **12.** Alle gedefinieerde functionele tests slagen.
- [ ] **13.** De kernfunctionaliteit is handmatig getest met een guerrilla-test.
- [ ] **14.** Er staan geen kritieke of hoogprioritaire bugs meer open.
- [ ] **15.** Voor alle businesslogica zijn unit tests geschreven (bijv. berekeningen).
- [ ] **16.** Frontend unit tests zijn geschreven voor logica met regels/condities (pure DB-aanroepen en UI-interacties uitgesloten).
- [ ] **17.** End-to-end tests zijn uitgevoerd.
- [ ] **18.** De minimale test coverage norm (**90%**) is behaald. Coverage: `___%`
- [ ] **19.** Nieuwe businesslogica is gedekt met tests.
- [ ] **20.** Toegepast: unit tests, integratietests, systeemtests en acceptatietests.
- [ ] **21.** Integratietests zijn uitgevoerd op alle relevante interfaces (API's, DB, externe diensten).
- [ ] **22.** Handmatige acceptatietesten zijn uitgevoerd door development of QA.
- [ ] **23.** Securitytests zijn uitgevoerd (bij stories die authenticatie/autorisatie raken).
- [ ] **24.** Regressietests zijn uitgevoerd op functionaliteit die mogelijk geraakt is.

### 4. Beveiliging & privacy (AVG)

- [ ] **25.** Volgt de beveiligingsrichtlijnen (TLS, hashing, encryptie, secrets-management).
- [ ] **26.** Geen gevoelige gegevens in foutmeldingen of logs.
- [ ] **27.** Dataverwerking voldoet aan de AVG: dataminimalisatie, bewuste opslaglocaties, correcte toegangsniveaus.
- [ ] **28.** Logging voldoet aan het beleid (geen gevoelige data, bewaartermijnen, audits).
- [ ] **29.** Geen persoonsgegevens in URL's, logs, foutmeldingen of browseropslag; alleen fictieve testdata gebruikt.
- [ ] **30.** Gedocumenteerd welke persoonsgegevens worden gebruikt en waarom; bewaartermijn bepaald.

### 5. Performance & stabiliteit

- [ ] **31.** Geen memory leaks of onnodig zware queries.

### 6. Bruikbaarheid & responsiviteit

- [ ] **32.** Getest op mobiele apparaten, tablets en desktops.
- [ ] **33.** De layout past zich correct aan op verschillende schermformaten.
- [ ] **34.** Alle interacties werken correct op alle ondersteunde apparaten.

### 7. Toegankelijkheid & User Experience

- [ ] **35.** Elke view volgt WCAG 2.2 niveau AA.
- [ ] **36.** Elke view is getest op het behalen van alle WCAG-criteria.
- [ ] **37.** UI-componenten sluiten aan bij het design system / de UX-richtlijnen (Figma).
- [ ] **38.** Foutmeldingen zijn duidelijk en generiek waar nodig (security).

### 8. Documentatie & kennisoverdracht

- [ ] **39.** Gebruikersdocumentatie is aangemaakt of bijgewerkt waar van toepassing.
- [ ] **40.** Nieuwe functionaliteit is duidelijk beschreven voor de eindgebruiker.
- [ ] **41.** Functionele documentatie is bijgewerkt (Confluence).
- [ ] **42.** API-documentatie is geüpdatet (Swagger).
- [ ] **43.** Release notes bevatten de wijzigingen/impact.
- [ ] **44.** Eventuele configuraties voor beheer zijn gedocumenteerd.

### 9. Deploybaarheid

- [ ] **45.** De build slaagt zonder fouten.
- [ ] **46.** De feature is opgenomen in de CI/CD pipeline zonder blockers.
- [ ] **47.** Configuratie (omgeving, secrets, permissies) is klaar voor productie/acceptatie.

### 10. Acceptatie / stakeholdervalidatie

- [ ] **48.** De Projectleider heeft de user story beoordeeld en geaccepteerd.
- [ ] **49.** De Projectleider heeft de functionaliteit getest, geaccepteerd en goedgekeurd.
- [ ] **50.** Afhankelijkheden met andere teams zijn gevalideerd of gecommuniceerd.

### 11. Geen open blockers

- [ ] **51.** Er staan geen kritieke bugs of blockers open voor deze user story.
- [ ] **52.** Resterende (non-blocking) verbeterpunten zijn als tickets geregistreerd. Tickets: <!-- links -->

---

## 🏁 Afronding

| Onderdeel                   | Gegevens           |
| --------------------------- | ------------------ |
| **Alle criteria afgevinkt** | ☐ Ja ☐ Nee         |
| **Status user story**       | ☐ Done ☐ Niet Done |
| **Naam teamlid (auteur)**   |                    |
| **Reviewer 1**              |                    |
| **Reviewer 2**              |                    |
| **Naam Projectleider**      |                    |
| **Datum afronding**         |                    |
| **Opmerkingen**             |                    |
