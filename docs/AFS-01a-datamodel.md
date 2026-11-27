# AFS-01a - Datamodel en verwijzingen aanmaken

Dit model ondersteunt de gegevens uit AFS-01a. Het staat los van het dashboard.
AFS-01a heeft een patiëntregister voor deze workflow. De koppeling met AFS-03
wordt afgestemd wanneer de branches samenkomen.

## Vastgelegde keuzes

- `Referral` bevat een momentopname van de afgesproken patientgegevens en de
  medische informatie die de huisarts voor deze verwijzing selecteert. Het model
  bevat geen volledig huisartsendossier, afspraak of tijdslot.
- Een nieuwe verwijzing krijgt een `PatientId`. Eén patiëntrecord per BSN bevat
  naam, postcode, huisnummer, optioneel telefoonnummer en e-mailadres. Het
  bestaande patiëntrecord blijft bij een volgende verwijzing ongewijzigd. Elke
  verwijzing behoudt de gegevens zoals opgegeven bij die aanvraag.
- De migratie behoudt historische verwijzingen. Hun `PatientId` blijft leeg
  totdat een koppeling met bestaande patiëntgegevens kan worden vastgesteld.
- Een nieuwe verwijzing krijgt `Status = Open` en een aanmaaktijd in UTC.
  Status en aanmaaktijd zijn geen invoervelden voor het toekomstige formulier.
- Alleen de bevestigde urgentie `Regular` (weergave: `Regulier`) is opgenomen.
  Andere urgenties en vervolgstatussen moeten nog worden afgestemd.
- Specialisme en behandelcode worden voor nieuwe verwijzingen gecontroleerd tegen
  de meegeleverde behandelcatalogus. De verwijzing bewaart de bijbehorende namen
  en codes als momentopname; bestaande verwijzingen worden niet herschreven.

## Gegevens van een verwijzing

De migratie naar postcode en huisnummer bewaart adres en woonplaats van oudere
verwijzingen in de bestaande kolommen. Bij nieuwe verwijzingen worden die
kolommen leeggelaten. De nieuwe velden blijven voor oudere verwijzingen leeg
totdat de gegevens uit een betrouwbare bron beschikbaar zijn.

Telefoonnummer en bijlagen zijn optioneel. De andere inhoudelijke velden
zijn verplicht. De maximale lengtes zijn technische ontwerpkeuzes,
geen aanvullende eisen uit het interview. Medische tekstvelden krijgen
`nvarchar(max)` om inhoud niet tot een korte omschrijving te beperken.

| Gegeven | Property | Opslag / maximale lengte |
| --- | --- | --- |
| Identificatie verwijzing | `Id` | `uniqueidentifier`, primaire sleutel |
| BSN | `Bsn` | `varchar(9)`, bewaart voorloopnullen; geen unieke sleutel |
| Naam | `PatientName` | 200 tekens |
| Postcode | `PatientPostalCode` | Verplicht voor nieuwe verwijzingen, maximaal 12 tekens |
| Huisnummer | `PatientHouseNumber` | Verplicht voor nieuwe verwijzingen, maximaal 20 tekens inclusief toevoeging |
| Telefoonnummer | `PatientPhoneNumber` | Optioneel, maximaal 32 tekens |
| E-mailadres | `PatientEmailAddress` | 254 tekens |
| Specialisme | `Specialty` | 100 tekens |
| Behandelcode | `TreatmentCode` | 50 tekens |
| Reden van verwijzing en vraagstelling | `ReasonForReferral` | `nvarchar(max)` |
| Losse vraagstelling uit oudere aanvragen | `ClinicalQuestion` | `nvarchar(max)`, leeg bij het nieuwe formulier |
| Medicatie | `Medication` | `nvarchar(max)` |
| Medische gegevens | `MedicalInformation` | `nvarchar(max)` |
| Allergieen | `Allergies` | `nvarchar(max)` |
| Urgentie | `Urgency` | enum, opgeslagen als tekst van maximaal 32 tekens |
| Status | `Status` | enum, opgeslagen als tekst; standaard `Open` |
| Aanmaaktijd | `CreatedAtUtc` | `datetimeoffset`, gezet bij aanmaken van het object |

De EF Core-configuratie registreert de tabellen `Referrals` en
`ReferralAttachments`. Een index op `Status` en `CreatedAtUtc` ondersteunt een
later overzicht van open verwijzingen op aanmaaktijd.

## Optionele bijlagen

Een verwijzing heeft nul of meer `ReferralAttachment`-records. Elke bijlage hoort
bij precies een verwijzing via de verplichte foreign key `ReferralId`.

| Property | Betekenis |
| --- | --- |
| `Id` | Eigen identificatie van de bijlage |
| `ReferralId` | Verwijzing waartoe de bijlage behoort |
| `FileName` | Oorspronkelijke bestandsnaam, maximaal 255 tekens |
| `ContentType` | Mediatype, maximaal 127 tekens |
| `StorageKey` | Interne sleutel voor bestandsopslag, maximaal 500 tekens |
| `SizeInBytes` | Bestandsgrootte als `long` |

Dit is uitsluitend metadata. De opslagvoorziening en uploadfunctie bestaan nog
niet. `StorageKey` moet later door de server worden toegekend en is geen door de
gebruiker aan te leveren bestandspad. De latere uploadfunctie moet afbeeldingen
en pdf's controleren, inclusief bestandstype, grootte en toegangsrechten.

Het verwijderen van een verwijzing verwijdert door de foreign key ook de
bijbehorende metadata. Het opruimen van de daadwerkelijke bestanden vereist
later aparte afhandeling in de opslagservice; er is nu geen verwijderendpoint.

## Grens met de volgende fasen

`InitialCreate` bevat inmiddels de tabellen voor verwijzingen en bijlagenmetadata.
`Program.cs` past migrations bij opstarten automatisch toe. Neem de migration en
modelsnapshot samen met het model op in de branch. De onderstaande afspraken
blijven nodig voor de verdere aansluiting op de rest van het project.

Nog af te stemmen:

1. De koppeling met het huisartsaccount en eventueel een gezamenlijke patient.
   De identiteit van de verwijzer moet straks uit de ingelogde gebruiker komen.
2. Eventuele aanvullende urgenties en de routering naar het bevoegde ziekenhuis.
   De bron voor specialismen en behandelcodes is inmiddels de aangeleverde CSV.
3. De opslagvoorziening, toegestane afbeeldingstypen en limieten voor bijlagen.

## Verwijzingen aanmaken (fase 2)

`POST /api/referrals` op de backend valideert een `CreateReferralRequest` en slaat
de verwijzing op via `IReferralService` en een EF Core-repository, beide scoped.
Bij succes volgt
HTTP 201 met uitsluitend `id`, `status` (`Open`) en `createdAtUtc`. Er is nog geen
GET-endpoint; daarom wordt geen Location-header naar een niet-bestaand detail
teruggegeven. Een volledig fictief voorbeeld staat in `Server/Server.http` en
kan ook via Swagger worden verstuurd.

De DTO gebruikt dezelfde validatieregels in de HTTP-pipeline en in de service.
Ontbrekende, null, lege en alleen uit witruimte bestaande verplichte velden worden
geweigerd met HTTP 400 en veldfouten onder `errors` (camelCase veldnamen).
De meldingen voor `bsn`, `specialty` en `treatmentCode` zijn respectievelijk
`Vul het BSN in`, `Kies een specialisme` en `Kies een behandelcode`.
Bij ongeldige invoer wordt niets opgeslagen. De veldlengtes volgen het datamodel;
een urgentie moet een bekende waarde zijn. BSN-controlecijfer en overige
formaatregels zijn nog geen onderdeel van deze stap.

Een onbekende behandelcode geeft `Kies een geldige behandelcode`. Een bestaande
code bij een ander of onbekend specialisme geeft `De behandelcode hoort niet bij
het gekozen specialisme`. Beide meldingen staan onder `errors.treatmentCode` en
er wordt niets opgeslagen. Bij geldige keuzes worden code en specialisme volgens
de catalogusspelling opgeslagen; hoofdletterverschillen en omliggende spaties in
de verwijzingsaanvraag worden daarbij genormaliseerd.

Onverwachte fouten worden centraal afgehandeld met HTTP 500 en `ProblemDetails`,
ook in Development. De foutmelding bevat een vaste Nederlandse uitleg en een
`traceId`, zonder exceptiontekst of aangeleverde patientgegevens. De centrale
afhandeling logt alleen een vaste foutmelding met deze technische trace-ID;
de standaard exceptiondiagnostiek van die afhandeling is uitgeschakeld om
exceptiontekst niet alsnog naar de logs te schrijven.

Niet-afgesproken JSON-velden worden geweigerd, waaronder een volledig dossier,
status, ID, aanmaaktijd, afspraak/tijdslot en interne opslagsleutels. Bijlagen
hebben nog geen uploadcontract en kunnen dus nog niet worden meegestuurd; een
verwijzing zonder bijlagen werkt wel. Het antwoord bevat geen patiëntgegevens.

Authenticatie is afhankelijk van de aparte user story PRI-03. Totdat die en de
toegangsregels zijn aangesloten, is deze aanmaakfunctie uitsluitend beschikbaar
bij `ASPNETCORE_ENVIRONMENT=Development`. In Staging en Production geeft het
endpoint HTTP 404, ook bij geldige invoer. Dit is een tijdelijke beperking en
geen vervanging voor authenticatie of autorisatie. Gebruik lokaal fictieve data.

De aansluiting van het formulier, het overzicht voor bevoegde ziekenhuisplanners, koppeling met
huisartsaccounts, autorisatie, auditlogging en bijlagenopslag volgen in de volgende
stappen. De zes acceptatiecriteria van AFS-01a zijn daarmee nog niet alle voltooid.
Bij de frontendkoppeling moet ook de proxyroute worden afgestemd: het endpoint
heeft zelf het `/api`-prefix, terwijl de bestaande nginx-configuratie dat verwijdert.

## Behandelcatalogus

De 100 zorgbehandelingen uit `zorgbehandelingen (1).csv` zijn overgenomen in
`Server/Data/SeedData/treatments.json`. Dit bestand is de versiebeheerbare bron
voor de tabel `Treatments` en wordt ingebed in de applicatie; de Downloads-map
van de ontwikkelaar is niet nodig bij opstarten of publiceren.

| CSV-kolom | Model / API-veld | Betekenis |
| --- | --- | --- |
| Zorgcode | `Code` / `code` | Unieke behandelcode, bijvoorbeeld `Z0005` |
| Omschrijving | `Description` / `description` | Zichtbare omschrijving |
| Specialisme | `Specialty` / `specialty` | Een van de 12 aangeleverde specialismen |
| Tijdsduur (kwartieren) | `DurationInQuarters` / `durationInQuarters` | Aantal kwartieren, dus 3 betekent 45 minuten |
| Kosten (€) | `CostEuro` / `costEuro` | Bedrag in euro, opgeslagen als `decimal(18,2)` |

Gelijke omschrijvingen met verschillende zorgcodes blijven afzonderlijke keuzes.
Zo hebben `Z0005` en `Z0072` allebei de omschrijving `Consult oren`, maar een
verschillende tijdsduur en prijs. Er wordt met deze gegevens geen afspraak of
factuur aangemaakt.

De migratie `AddTreatmentCatalog` voegt uitsluitend de tabel, een index op
specialisme en de referentiegegevens toe. De bestaande opstartmigraties passen
dit toe wanneer de backend opnieuw start. Omdat EF Core deze vaste gegevens met
`HasData` beheert, worden ze niet bij iedere opstart opnieuw ingevoegd.
Maak bij toekomstige wijzigingen in `treatments.json` een nieuwe migratie;
bewerk reeds toegepaste migraties niet. Pas de catalogus niet los hiervan
handmatig in de database aan.

Beschikbare endpoints voor het toekomstige formulier:

- `GET /api/treatments/specialties`: alfabetische lijst met beschikbare specialismen.
- `GET /api/treatments?specialty=KNO`: behandelingen voor het geselecteerde
  specialisme, gesorteerd op omschrijving en vervolgens code. Gebruik de
  specialismenaam zoals teruggegeven door het eerste endpoint.

Een ontbrekend of leeg specialisme geeft HTTP 400. Een onbekend specialisme geeft
een lege lijst. De endpoints bevatten uitsluitend referentiegegevens en zijn
ook buiten Development beschikbaar. De beperkingen voor het aanmaken van
verwijzingen blijven gelden. Voorbeelden staan in `Server/Server.http` en Swagger.

De verwijzingsaanvraag houdt de bestaande velden `specialty` en `treatmentCode`.
Voor KNO kan bijvoorbeeld `Z0005` worden gebruikt; de oude voorbeeldcode `TEST-KNO`
is geen geldige cataloguskeuze meer. Er is bewust geen foreign key vanuit oude
verwijzingen toegevoegd: hun historische gegevens moeten intact blijven wanneer
de catalogus later verandert.

## Verwijzingsformulier

Via **Verwijsbrieven > Nieuwe verwijzing** opent het formulier binnen het dashboard.
Specialismen en behandelingen komen uit de API. Wisselen van specialisme wist de
behandelkeuze; oudere aanvragen worden geannuleerd. Behandelingen tonen hun code.

Met **Zoek patiënt** kunnen ingelogde huisartsen ieder bekend BSN van negen cijfers
exact opzoeken via `POST /api/referrals/patient-lookup`. Het BSN staat in de body.
Het antwoord bevat alleen contactgegevens en wordt niet gecachet. Bij een
onbekend BSN vult de huisarts de gegevens zelf in en worden bij opslaan de patiënt
en de verwijzing samen opgeslagen. De API weigert anonieme aanvragen en andere
rollen. Voor gebruik met echte logins moeten de PRI-03/04-authenticatiemiddleware
en het doorgeven van het geldige toegangstoken in de frontend worden aangesloten.

Het formulier verstuurt de invoer met POST naar /api/referrals. Tijdens het opslaan
zijn de invoervelden en verzendknop uitgeschakeld. Alleen HTTP 201 geldt als
bevestiging; daarna verdwijnt de invoer en verschijnt status Open. Bij een fout
blijft de invoer behouden en verschijnt een veilige melding, eventueel met de
namen van te controleren velden. Een verbindingsfout kan betekenen dat de server
wel heeft opgeslagen; de melding vraagt daarom eerst de opslag te controleren.
Er is nog geen idempotentiesleutel of overzicht om deze onzekerheid op te lossen.

Patiëntgegevens worden niet in URL's, logs of browseropslag gezet. Gebruik fictieve
gegevens; de backend staat aanmaken uitsluitend in Development toe totdat PRI-03
is aangesloten. Lokale Vite-aanvragen naar /api gaan naar localhost:5174.
Bijlagen, accounts en het overzicht voor planners volgen afzonderlijk.

## Privacy en oplevering

BSN en naam identificeren de patient; postcode, huisnummer, telefoonnummer en
e-mailadres ondersteunen contact door de ontvangende zorgverlener. De geselecteerde
medische informatie, medicatie, allergieen, reden en vraagstelling ondersteunen de
beoordeling van de verwijzing. Specialisme, behandelcode en urgentie ondersteunen
de routering en planning. De tabellen hierboven beschrijven de opgeslagen velden;
het volledige huisartsendossier wordt niet overgenomen.

De bewaartermijn en de daarop aansluitende verwijder- of anonimiseringsregels
zijn nog niet door het team vastgesteld. Er is daarom geen termijn verzonnen
en nog geen automatische opschoonfunctie toegevoegd. Leg deze beslissing vast
en vermeld gegevens, doel en termijn in de AVG-sectie van de uiteindelijke PR.

Dit is een tussenstap en geen afgeronde user story volgens de DoD. Voor oplevering
blijven onder meer PRI-03 met rolgebonden toegang, de resterende acceptatiecriteria,
de hoofdflow als end-to-end test, een guerrilla-test en de vereiste reviews en
acceptatie nodig. De DoD vraagt minimaal 90% test coverage; dekking van alleen de
verwijzingsservice bewijst niet dat het hele project aan die norm voldoet.
Stem ontbrekende keuzes en afhankelijkheden af voordat de vervolgtaak volgens
de DoR als Ready wordt ingepland.

## Controle

`Server.Tests/ReferralModelTests.cs` controleert de SQL Server-schemaopbouw zonder
databaseverbinding en de EF Core-koppeling van bijlagen bij twee verwijzingen met
hetzelfde BSN.

`ReferralServiceTests` controleert validatie, serverwaarden en het voorkomen van
opslag bij fouten met een lokale testimplementatie van de repository.
`CreateReferralApiTests` gebruikt de echte HTTP-pipeline, service en repository
met een afzonderlijke SQLite-database in het geheugen per test. Opslag wordt met
een nieuwe DbContext teruggelezen. Deze tests gebruiken geen draaiende API,
browser, LocalDB of ontwikkeldatabase. Ze bewijzen geen SQL Server-specifieke
migratie-uitvoering; daarvoor blijft een aparte SQL Server-integratiecontrole nodig.
De API-tests simuleren ook een opslagfout met fictieve patientgegevens in de
exception en controleren dat de HTTP-respons en de centrale foutlog deze niet
overnemen, ook wanneer een browser een HTML-foutpagina vraagt.
De testhost schrijft uitsluitend naar de testuitvoer en vereist geen toegang
tot het Windows-systeemlogboek.

`TreatmentCatalogApiTests` controleert alle 100 codes via de 12 specialismen,
behoud van omschrijvingen, tijdsduur en bedragen, filters en ontbrekende invoer.
De tests controleren ook dat onbekende codes en onjuiste code-specialismecombinaties
worden geweigerd en een gecorrigeerde verwijzing daarna wel opgeslagen kan worden.
De migratie `AddTreatmentCatalog` is daarnaast gecontroleerd op een afzonderlijke
SQL Server-database: upgrade vanaf `InitialCreate`, behoud van een bestaande
verwijzing met een oude code, herhaald uitvoeren en terugdraaien/opnieuw toepassen.
De tijdelijke controledatabase is na afloop verwijderd.

De browserbasis `TestBase` is abstract. Alleen afgeleide EndToEnd-tests voeren
de initialisatie van SQL Server, een externe app en Chrome uit. Die tests draaien
niet parallel en zijn uitgesloten van de standaard CI-job. De bestaande
browserhelpers zijn geen onderdeel van de nieuwe unit- en API-tests.

De API-testhost schakelt startup-migrations uit met
`Database:ApplyMigrationsOnStartup=false` en maakt zelf zijn SQLite-schema aan.
Voor normaal starten blijft de standaardwaarde `true`.

```powershell
dotnet test Server/Server.slnx --configuration Release --filter "TestCategory!=EndToEnd"
```

Stop een zelf gestarte Debug-backend met Ctrl+C voordat je die opnieuw bouwt of
start. De bovenstaande testopdracht bouwt Release en gebruikt geen luisterpoort.

## Uploadfase

Het formulier accepteert optioneel PDF, JPG/JPEG en PNG: maximaal 5 bestanden,
10 MiB per bestand en 25 MiB samen. Deze limieten zijn afgestemde ontwerpkeuzes.
Met bijlagen verstuurt de client multipart naar POST /api/referrals/with-attachments:
het veld referral bevat het bestaande JSON-contract, attachments bevat de bestanden.
Zonder bestanden blijft het bestaande JSON-endpoint werken.

De server valideert aantallen, lengtes, bestandsnaam, extensie en bestandskenmerken
(signature en eindmarkering). Dit is geen volledige documentparser of virusscanner.
Uploads blijven Development-only. Bestanden zijn niet openbaar te downloaden;
rolgebonden toegang en de definitieve opslagvoorziening hangen af van PRI-03.

Standaard staan bestanden onder LocalApplicationData/MediSharp/attachments, buiten
Git en wwwroot. Attachments:StoragePath kan een afgeschermde absolute opslagmap
instellen. Configureer die buiten de webmap, met beperkte bestandsrechten en een
blijvend volume bij hosting. De database bevat uitsluitend metadata en interne sleutels.

Bestanden worden onder gegenereerde namen geschreven, waarna verwijzing en metadata
in één SaveChanges worden vastgelegd. Validatiefouten en opslagfouten ruimen de
bestanden van die aanvraag op. Een procescrash tussen bestandsschrijven en databaseopslag
kan verweesde bestanden achterlaten; er is nog geen periodieke opruimtaak.
Er is geen databasewijziging nodig: de bestaande bijlagentabel wordt gebruikt.

Upload-integratietests gebruiken een tijdelijke map en een geïsoleerde database.
Ze controleren bytes, metadata, limieten, geweigerde bestandstypen en opruiming.
