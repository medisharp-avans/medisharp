
<!--
  MediSharp – Pull Request
  Vink alleen aan wat je echt hebt gecontroleerd. Niet van toepassing? Laat leeg en zet "n.v.t." erachter.

  Nummers:
  - DoD x = punt x uit de Definition of Done (v1.2)
  - CR x.y = punt x.y uit de code review-checklist (CODE_REVIEW.md)

  De overige DoD-punten (1, 3, 8, 12, 14, 17, 29, 31–36) gaan over de hele user story
  en worden na de merge afgevinkt op de Trello-kaart.
-->

## 🔗 User story

**Trello-kaart:** 
**User story:** 

## 📝 Wat en waarom

<!-- Wat verandert er en waarom? Benoem de belangrijkste technische keuzes. -->

## 🧪 Hoe kan de reviewer dit testen?

<!-- Stappen om het lokaal te controleren. Welk testaccount / welke testdata? -->

## 🖼️ Screenshots

<!-- Alleen bij UI-wijzigingen: mobiel + desktop. Uitsluitend fictieve data. -->

## Type wijziging

- [ ] ✨ Feature
- [ ] 🐛 Fix / hotfix
- [ ] 🧹 Chore (refactor, packages, config)
- [ ] 🗄️ Database / migratie
- [ ] 📚 Documentatie
- [ ] ⚙️ CI/CD

## 🔒 Persoonsgegevens (AVG) – DoD 20

<!-- Welke persoonsgegevens worden verwerkt, waarom, en wat is de bewaartermijn? Anders: "Geen". -->

---

## ✅ Checklist auteur

### Altijd

- [ ] **CR 4.2** – Branchnaam en PR-titel volgen CONTRIBUTING.md (bijv. `feature/AFS-03-schedule-appointment`, _"AFS-03: Patient can schedule an appointment"_)
- [ ] **DoD 2** – Edge cases zijn afgevangen (foutieve invoer, ontbrekende rechten, verlopen sessie)
- [ ] **DoD 4** – De code volgt de coderingsstandaarden (CODE_REVIEW.md)
- [ ] **DoD 5** – Linting en formattering geven geen fouten
- [ ] **DoD 6** – Methoden, endpoints en ingewikkelde logica zijn gedocumenteerd
- [ ] **DoD 9** – Alle tests slagen
- [ ] **DoD 10–11** – Nieuwe businesslogica (backend) en frontend-logica met condities is gedekt met unit tests
- [ ] **DoD 15** – De kernfunctionaliteit is handmatig getest
- [ ] **DoD 18** – Geen persoonsgegevens in URL's, logs, foutmeldingen of browseropslag
- [ ] **DoD 19** – Alleen fictieve testdata gebruikt, ook in seeders en tests
- [ ] **DoD 20** – De AVG-sectie hierboven is ingevuld
- [ ] **DoD 22** – Geen onnodig zware queries (N+1, ophalen zonder filter of paginering)

### Indien van toepassing

- [ ] **DoD 7** – Database: EF Core-migratie toegevoegd en getest
- [ ] **DoD 13** – API: integratietests voor nieuwe of gewijzigde endpoints
- [ ] **DoD 16, 21** – Autorisatie: getest dat gebruikers zonder de juiste rechten geen toegang krijgen
- [ ] **DoD 23–24** – UI: getest op mobiel, tablet en desktop en sluit aan bij Figma
- [ ] **DoD 25–26** – UI: voldoet aan WCAG 2.2 AA, gecontroleerd met Lighthouse/axe en met alleen het toetsenbord
- [ ] **DoD 27** – UI: foutmeldingen zijn duidelijk en tonen geen technische details
- [ ] **DoD 28** – API: Swagger/OpenAPI is bijgewerkt
- [ ] **DoD 30** – Nieuwe configuratie (omgevingsvariabelen, secrets) is gedocumenteerd

## 📌 Open punten

<!-- Bekende beperkingen of vervolgwerk, met links naar Trello-kaarten (DoD 36). Anders: "Geen". -->

---

> **Reviewers:** beoordeel deze PR met de [code review-checklist](https://github.com/medisharp-avans/medisharp/blob/development/CODE_REVIEW.md), verwijs in comments naar het nummer (bijv. _"2.4"_) en geef je oordeel via **Review changes**.
