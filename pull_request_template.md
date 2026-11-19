<!--
  MediSharp – Pull Request
  Vink alleen aan wat je echt hebt gecontroleerd.
  Niet van toepassing? Laat leeg en zet "n.v.t." erachter.
  De volledige Definition of Done staat op de Trello-kaart van de user story
  en wordt daar afgevinkt, niet hier.
-->

## 🔗 User story

**Trello-kaart:** <!-- link naar de kaart -->
**User story:** <!-- bijv. HUI-1 – Verwijsbrief aanmaken -->

## 📝 Wat en waarom

<!-- Wat verandert er en waarom? Benoem de belangrijkste technische keuzes. -->

## 🧪 Hoe kan de reviewer dit testen?

<!-- Stappen om het lokaal te controleren. Welk testaccount / welke testdata? -->

1.

## 🖼️ Screenshots

<!-- Alleen bij UI-wijzigingen: mobiel + desktop. Uitsluitend fictieve data. -->

## Type wijziging

- [ ] ✨ Feature
- [ ] 🐛 Bugfix
- [ ] ♻️ Refactor
- [ ] 🗄️ Database / migratie
- [ ] 📚 Documentatie
- [ ] ⚙️ Configuratie / CI/CD

## 🔒 Persoonsgegevens (AVG)

<!-- Welke persoonsgegevens worden verwerkt, waarom, en wat is de bewaartermijn? Anders: "Geen". -->

---

## ✅ Checklist auteur

### Altijd

- [ ] Branchnaam en PR-titel volgen CONTRIBUTING.md (bijv. `feature/AFS-03-schedule-appointment`, _"AFS-03: Patient can schedule an appointment"_)
- [ ] Code volgt de coderingsstandaarden (Controller → Service → Repository, DTO's, async/await, Composition API)
- [ ] Edge cases zijn afgevangen (foutieve invoer, ontbrekende rechten, verlopen sessie)
- [ ] Nieuwe businesslogica is gedekt met unit tests en alle tests slagen lokaal
- [ ] Geen persoonsgegevens of gevoelige info in logs, URL's, foutmeldingen of browseropslag
- [ ] Geen secrets gecommit; alleen fictieve testdata gebruikt
- [ ] Ingewikkelde logica en nieuwe endpoints zijn gedocumenteerd

### Indien van toepassing

- [ ] **API:** Swagger klopt en beschermde endpoints hebben `[Authorize]`
- [ ] **Database:** migratie toegevoegd en lokaal toegepast
- [ ] **Autorisatie:** getest met een gebruiker _zonder_ de juiste rechten
- [ ] **UI:** getest op mobiel, tablet en desktop en sluit aan bij Figma
- [ ] **UI:** WCAG 2.2 AA gecontroleerd (toetsenbord, focus zichtbaar, contrast, labels)

## 📌 Open punten

<!-- Bekende beperkingen of vervolgwerk, met links naar Trello-kaarten. Anders: "Geen". -->

---

> **Reviewers:** beoordeel deze PR met de [code review-checklist](https://github.com/medisharp-avans/medisharp/blob/development/CODE_REVIEW.md) en geef je oordeel via **Review changes**.
