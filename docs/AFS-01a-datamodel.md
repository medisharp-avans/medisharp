# AFS-01a - Fase 1: datamodel voor verwijzingen

Dit model ondersteunt de gegevens uit AFS-01a. Het staat los van het dashboard.
Het team heeft bevestigd dat er nog geen gezamenlijk patientmodel is afgesproken.

## Vastgelegde keuzes

- `Referral` bevat een momentopname van de afgesproken patientgegevens en de
  medische informatie die de huisarts voor deze verwijzing selecteert. Het model
  bevat geen volledig huisartsendossier, afspraak of tijdslot.
- Er is geen aparte `Patient`-tabel. Meerdere verwijzingen met hetzelfde BSN zijn
  toegestaan en behouden elk hun eigen gegevens. Een eventuele latere koppeling
  met een gezamenlijk patientmodel vervangt deze momentopname niet automatisch.
- Een nieuwe verwijzing krijgt `Status = Open` en een aanmaaktijd in UTC.
  Status en aanmaaktijd zijn geen invoervelden voor het toekomstige formulier.
- Alleen de bevestigde urgentie `Regular` (weergave: `Regulier`) is opgenomen.
  Andere urgenties en vervolgstatussen moeten nog worden afgestemd.
- Specialisme en behandelcode zijn voorlopig tekstvelden. Er worden nog geen
  gedeelde keuzetabellen of verzonnen behandelcodes ingevoerd.

## Gegevens van een verwijzing

Alle onderstaande inhoudelijke velden zijn verplicht in het model; alleen
bijlagen zijn optioneel. De maximale lengtes zijn technische ontwerpkeuzes,
geen aanvullende eisen uit het interview. Medische tekstvelden krijgen
`nvarchar(max)` om inhoud niet tot een korte omschrijving te beperken.

| Gegeven | Property | Opslag / maximale lengte |
| --- | --- | --- |
| Identificatie verwijzing | `Id` | `uniqueidentifier`, primaire sleutel |
| BSN | `Bsn` | `varchar(9)`, bewaart voorloopnullen; geen unieke sleutel |
| Naam | `PatientName` | 200 tekens |
| Adres | `PatientAddress` | 300 tekens |
| Woonplaats | `PatientCity` | 100 tekens |
| Telefoonnummer | `PatientPhoneNumber` | 32 tekens |
| E-mailadres | `PatientEmailAddress` | 254 tekens |
| Specialisme | `Specialty` | 100 tekens |
| Behandelcode | `TreatmentCode` | 50 tekens |
| Reden van verwijzing | `ReasonForReferral` | `nvarchar(max)` |
| Vraagstelling | `ClinicalQuestion` | `nvarchar(max)` |
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

Er zijn nog geen migrations aangemaakt of op een database uitgevoerd. Stem het
gedeelde model en onderstaande punten af voordat de definitieve migration wordt
gegenereerd. `Program.cs` past migrations bij opstarten automatisch toe; deze
wijziging alleen maakt de nieuwe tabellen dus nog niet aan.

Nog af te stemmen:

1. De koppeling met het huisartsaccount en eventueel een gezamenlijke patient.
   De identiteit van de verwijzer moet straks uit de ingelogde gebruiker komen.
2. De bron van de lijsten met specialismen, behandelcodes en eventuele aanvullende
   urgenties. Ook de routering naar het bevoegde ziekenhuis moet worden bepaald.
3. De opslagvoorziening, toegestane afbeeldingstypen en limieten voor bijlagen.

Voor fase 2 blijven `POST /api/referrals` en `GET /api/referrals` het voorstel uit
het gesprek. DTO's, controllers en services worden in die fase uitgewerkt. Maak
expliciete DTO's voor de afgesproken invoervelden en geef interne entiteiten of
opslagsleutels niet rechtstreeks terug. De huisarts kiest geen afspraak of tijdslot.

`required` in C# en `IsRequired` in EF Core zijn geen volledige invoervalidatie:
lege of ongeldige waarden worden hiermee niet allemaal tegengehouden. De volgende
fasen moeten onder meer de meldingen `Kies een specialisme`, `Vul het BSN in` en
`Kies een behandelcode` toevoegen en ervoor zorgen dat ongeldige invoer niet wordt
opgeslagen. Autorisatie en een overzicht voor ziekenhuispersoneel volgen eveneens
later. De zes acceptatiecriteria zijn met alleen dit datamodel nog niet voltooid.

## Controle

`Server.Tests/ReferralModelTests.cs` controleert de SQL Server-schemaopbouw zonder
databaseverbinding en de EF Core-koppeling van bijlagen bij twee verwijzingen met
hetzelfde BSN. Dit controleert de mapping, niet daadwerkelijk opslaan of ophalen
uit SQL Server en niet de toekomstige API-validatie of autorisatie.

```powershell
dotnet build Server/Server.slnx --no-restore --configuration Release
dotnet test Server/Server.slnx --no-build --configuration Release
```
