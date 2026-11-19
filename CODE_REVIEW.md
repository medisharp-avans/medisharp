
# Code review-checklist

Gebruik deze lijst als reviewer bij **elke pull request**. Het is een naslagdocument, geen formulier: je vinkt hier niets af.

## Zo review je

1. Lees de PR-beschrijving: wat is er veranderd, en hoe kun je het testen?
2. Loop de gewijzigde bestanden door (tab **Files changed**) met deze lijst ernaast.
3. Zie je iets dat niet klopt? Plaats een comment op die regel en noem het nummer, bijvoorbeeld:
   > **2.4** – Hier wordt `.Result` gebruikt; maak deze methode `async`.
   >
4. Rond af via **Review changes**:
   - **Approve** – alles klopt (kleine opmerkingen mogen, begin die met `nit:`)
   - **Request changes** – er moet iets aangepast worden voordat het gemerged mag worden
   - **Comment** – alleen een vraag, nog geen oordeel

---

## 1. Algemene codeerprincipes

- **1.1** Code is leesbaar, gestructureerd en onderhoudbaar.
- **1.2** Functies en klassen hebben commentaar dat ze toelicht.
- **1.3** Variabelen, functies en klassen hebben duidelijke, beschrijvende namen.
- **1.4** Er is geen dubbele code (DRY).
- **1.5** Functies en klassen hebben één verantwoordelijkheid.

## 2. ASP.NET Core

- **2.1** Lagen worden gerespecteerd: Controller → Service → Repository / DbContext. Controllers blijven dun en bevatten geen businesslogica.
- **2.2** Requests en responses gebruiken DTO's; entiteiten worden nooit naar buiten gegeven. Invoer wordt gevalideerd voordat die de businesslogica bereikt.
- **2.3** Dependencies worden via de constructor geïnjecteerd (interfaces geregistreerd in `Program.cs`).
- **2.4** Alle I/O gebruikt `async`/`await` (geen `.Result` of `.Wait()`).
- **2.5** Juiste HTTP-verbs en statuscodes; fouten worden centraal afgehandeld (`ProblemDetails`).
- **2.6** EF Core-wijzigingen gaan via migraties en queries zijn geparametriseerd.
- **2.7** Er zijn geen secrets gecommit; beschermde endpoints hebben `[Authorize]`.
- **2.8** C#-naamgeving: `PascalCase` (klassen, methoden, properties), `camelCase` (parameters, lokale variabelen), `_camelCase` (private velden), `IName` (interfaces).

## 3. Vue

- **3.1** Vue 3 met de Composition API (`<script setup>`) in Single-File Components met meerwoordige `PascalCase`-namen.
- **3.2** De vaste mappenstructuur wordt gevolgd: `views/`, `components/`, `composables/`, `stores/`, `services/`, `router/`.
- **3.3** Props naar beneden, events naar boven; props worden nooit gemuteerd.
- **3.4** Gedeelde state staat in Pinia, herbruikbare logica in composables en API-aanroepen in de servicelaag (niet in componenten).
- **3.5** Modern JavaScript (ES6+); geen globale variabelen en geen directe DOM-manipulatie.
- **3.6** Duidelijke scheiding tussen state, presentatie (template) en event handling.
- **3.7** Code is gecontroleerd met ESLint en geformatteerd met Prettier.

## 4. Git

- **4.1** De wijziging staat in een eigen branch voor deze feature of user story.
- **4.2** De branchnaam volgt de afspraken in [CONTRIBUTING.md](CONTRIBUTING.md#naming): `type/<STORY-ID>-korte-beschrijving`, bijv. `feature/AFS-03-schedule-appointment`.
- **4.3** Commitberichten zijn duidelijk en beschrijvend (bijv. _"Voeg validatie toe aan verwijsbriefformulier"_).

## 5. UI/UX

- **5.1** De interface volgt het afgesproken ontwerp (Figma waar van toepassing).
- **5.2** Layout, spacing en UI-componenten worden consistent gebruikt.
- **5.3** Interacties zijn intuïtief en consistent met de rest van de applicatie.
- **5.4** De view is toegankelijk: te bedienen met het toetsenbord, zichtbare focus, labels bij invoervelden, voldoende contrast (WCAG 2.2 AA).

## 6. AVG

- **6.1** Er staan geen persoonsgegevens in URL's, logs, foutmeldingen of browseropslag.
- **6.2** Er wordt alleen fictieve testdata gebruikt (ook in seeders en tests).
- **6.3** De AVG-sectie in de PR is ingevuld en klopt met wat de code doet (welke gegevens, waarom, bewaartermijn).
