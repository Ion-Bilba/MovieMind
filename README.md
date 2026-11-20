# MovieMind — ghid de reluare a proiectului

Actualizat: 5 noiembrie 2026. Branch-ul de integrare al MVP-ului: `Dev`.

## 1. Cum pornești proiectul data viitoare

Ai nevoie de .NET 10 SDK. Rulează din directorul principal al proiectului:

În Visual Studio pornește proiectele **MovieMind.API** și **MovieMind.Web** simultan
(Multiple startup projects). Din două terminale, din folderul soluției:

```powershell
dotnet run --project MovieMind --launch-profile http
dotnet run --project MovieMind.Web --launch-profile http
```

API-ul rulează la **http://localhost:5048** (Swagger: **/swagger**), iar interfața la
**http://localhost:5104**. Oprește ambele procese cu **Ctrl+C** în terminalele lor.
`MovieMind.Web` servește doar interfața; toate endpoint-urile și autentificarea sunt
în proiectul **MovieMind.API**.

În Development, migrațiile SQLite se aplică automat la pornire. Dacă este prima
pornire pe un calculator nou, se creează o bază goală în `App_Data/moviemind.db`.
Pentru filme demonstrative, apasă **Load demo collection** în interfață.

După modificări în cod, repornește aplicația. Dacă vezi interfața veche, fă un
hard refresh în browser: **⌘⇧R** pe Mac.
Dacă apare „address already in use”, oprește instanța care rulează deja pe portul
5104 înainte de a porni alta.

## 2. Starea proiectului pentru următoarea sesiune

Implementat:

- Designul din `origin/feature/frontend-setup`: catalog, detalii, watchlist și Studio.
- Salvarea, citirea, editarea și ștergerea filmelor prin backend, în baza de date.
- Persistența tuturor câmpurilor formularului, inclusiv genuri, scenariști,
  distribuție cu roluri, premii, țară, limbă și configurația posterului generat.
- Selectarea și drag-and-drop pentru postere JPG, PNG sau WebP de maximum 10 MB.
  Browserul redimensionează imaginea la maximum 520 × 780 înainte de upload.
- Zona „Choose an image” este acoperită de un input nativ. Panoul posterului se
  derulează cu formularul, pentru a nu ascunde butonul sub footer pe ferestre mici.
- Mesaje pentru upload/salvare eșuată; formularul rămâne deschis dacă salvarea eșuează.
- Migrații distincte pentru SQLite și SQL Server și teste de integrare.

De reținut:

- **Filmele și metadatele sunt în baza de date.** Repornirea aplicației sau ștergerea
  datelor browserului nu le șterge.
- Imaginile încărcate sunt fișiere în `App_Data/posters/`; în tabel se salvează URL-ul.
  După upload trebuie să salvezi și formularul filmului.
- Watchlist-ul, starea „văzut” și tema rămân în browser, separat de catalogul comun.
- Preferințele utilizatorului sunt salvate pe genuri și expuse împreună cu vectorul folosit de recomandări.
- Autentificarea JWT este implementată în `MovieMind.API`: register, login și role claims.
  Interfața trimite tokenul în antetul Bearer. Scrierea catalogului și upload-ul sunt
  permise doar rolului `Admin`; conturile noi primesc rolul `User`.
- Imaginile nefolosite sunt păstrate și după anularea unui formular sau înlocuirea
  unui poster; nu există încă o operație automată de curățare.
- SQL Server are migrațiile generate și verificate ca model/script, dar execuția lor
  nu a fost testată pe un server SQL Server disponibil.

Dacă ai filme din versiunea veche, salvate doar în browser, folosește
**Studio → Import browser films**. Importul este explicit și nu șterge datele vechi
ale browserului. **Import/Export** lucrează cu fișiere JSON pentru metadatele filmelor.

## 3. Unde sunt baza de date și tabelele

Configurația locală implicită folosește **SQLite**, nu SQL Server:

```text
MovieMind/
  App_Data/
    moviemind.db       ← baza de date
    posters/          ← imaginile încărcate
```

`App_Data/` este exclus din Git: un commit păstrează codul și migrațiile, nu filmele
sau imaginile tale locale. La clonarea proiectului pe alt calculator, copiază și
acest director dacă vrei să păstrezi colecția.

Dacă setezi `Storage__DataDirectory` sau `ConnectionStrings__Sqlite`, verifică acea
locație; baza activă poate fi diferită de calea implicită de mai sus.

### Vizualizare în terminal

Deschide un al doilea terminal în directorul proiectului:

```sh
sqlite3 -readonly App_Data/moviemind.db
```

Apoi introdu comenzile următoare, pe rând:

```text
.tables
.schema Movies
.headers on
.mode column
SELECT Id, Title, ReleaseYear, Duration, CatalogRating FROM Movies;
SELECT Title, PosterUrl FROM Movies;
SELECT Id, Title FROM Movies WHERE lower(Title) = 'asd';
.quit
```

`-readonly` permite inspectarea fără modificarea datelor. SQL-ul se încheie cu `;`,
iar comenzile care încep cu punct, precum `.tables`, se scriu pe linii separate.
[Documentația oficială SQLite CLI](https://sqlite.org/cli.html).

Poți rula și o singură interogare, fără sesiune interactivă:

```sh
sqlite3 -readonly -header -column App_Data/moviemind.db 'SELECT Title, ReleaseYear, CatalogRating FROM Movies;'
```

### Vizualizare într-o aplicație cu interfață grafică

[DB Browser for SQLite](https://sqlitebrowser.org/) permite vizualizarea și
interogarea fișierelor SQLite, inclusiv pe macOS.

1. Deschide aplicația și alege **Open Database**.
2. Selectează `/Users/muntean/MovieMind/App_Data/moviemind.db`.
3. În **Browse Data**, alege tabelul `Movies`, `Genres` sau alt tabel.
4. Folosește **Execute SQL** pentru interogările de mai jos.

Pentru modificări obișnuite ale filmelor, folosește **Studio** din MovieMind:
backend-ul actualizează și relațiile dintre tabele.

### Ce conține fiecare tabel

| Tabel | Conținut |
| --- | --- |
| `Movies` | Filmul și detaliile lui: titlu, an, durată, descriere, oameni, poster etc. |
| `Genres` | Genurile disponibile în baza de date. |
| `MovieGenres` | Legăturile dintre filme și genuri. |
| `Actors` | Actorii folosiți în distribuție. |
| `MovieActors` | Filmul, actorul, personajul (`CharacterName`) și ordinea (`Position`). |
| `Users` | Conturi, hash-uri de parolă și roluri; parola în clar nu se salvează. |
| `Ratings` | Structura pentru evaluările utilizatorilor, cu scoruri de la 0 la 10. |
| `UserPreferenceProfiles` | Profilul preferințelor fiecărui utilizator și data ultimei actualizări. |
| `UserGenrePreferences` | Preferințele explicite (0–10) ale unui utilizator pentru genuri. |
| `__EFMigrationsHistory` | Migrațiile deja aplicate bazei de date. |
| `__EFMigrationsLock` | Tabel tehnic folosit de EF la aplicarea migrațiilor SQLite. |

Câteva denumiri diferă între formular și tabelul `Movies`:

| Formular / JSON | Coloană în `Movies` |
| --- | --- |
| `year` | `ReleaseYear` |
| `synopsis` | `Description` |
| `rating` (0–10) | `CatalogRating` |
| `poster` | `PosterUrl` |
| `writers` | `Writers`, listă stocată ca JSON |
| `art.palette`, `art.motif` | `ArtPalette`, `ArtMotif` |

`AverageRating` și `RatingCount` sunt separate de ratingul manual al catalogului;
nu reprezintă câmpul 0–10 din formular.

Interogare pentru genurile filmelor:

```sql
SELECT m.Title, g.Name AS Genre
FROM Movies AS m
JOIN MovieGenres AS mg ON mg.MovieId = m.Id
JOIN Genres AS g ON g.Id = mg.GenreId
ORDER BY m.Title, g.Name;
```

Interogare pentru distribuție și personaje:

```sql
SELECT m.Title, a.Name AS Actor, ma.CharacterName AS Character
FROM Movies AS m
JOIN MovieActors AS ma ON ma.MovieId = m.Id
JOIN Actors AS a ON a.Id = ma.ActorId
ORDER BY m.Title, ma.Position;
```

Dacă aceste interogări nu întorc rânduri, verifică dacă ai adăugat filme și dacă
le-ai atribuit genuri sau actori. Tabelele nu se populează automat cu date demo.

## 4. Backend și API

Lista filmelor poate fi văzută direct în browser, cu aplicația pornită:
**http://localhost:5104/api/films**.

| Rută | Funcție |
| --- | --- |
| `GET /api/films` | Lista filmelor. |
| `GET /api/users/me/preferences` | Preferințele și vectorul utilizatorului autentificat. |
| `PUT /api/users/me/preferences` | Înlocuiește preferințele explicite; corpul conține `genrePreferences` cu `genreId` și scor 0–10. O listă goală le șterge. |
| `GET /api/users/me/preferences/vector` | Vectorul utilizatorului: o valoare pentru fiecare gen. |
| `GET /api/recommendations?count=10` | Recomandări personalizate pentru utilizatorul autentificat; `count` acceptă valori între 1 și 50. |
| `GET /api/films/{id}` | Detaliile unui film. |
| `PUT /api/films/{id}` | Creare sau actualizare cu documentul JSON complet. |
| `DELETE /api/films/{id}` | Ștergerea filmului și a legăturilor cu genuri/actori. |
| `POST /api/posters` | Upload multipart, câmpul `file`; răspunde cu URL-ul imaginii. |
| `GET /media/posters/{name}` | Imagine încărcată pe server. |

### Algoritmii APA (etapa 8)

Implementările sunt funcții pure în `MovieMind.Application/Algorithms`, reutilizabile
de motorul de recomandări din etapa 9:

- `SparsePreferenceVector` păstrează afinitățile `-1..1` indexate după ID-ul genului;
  genurile care lipsesc dintr-un vector sunt tratate ca `0` (neutru). `FromGenres`
  creează un vector binar pentru un film.
- `CosineSimilarity.Calculate` calculează similaritatea între doi vectori rari.
  Întoarce `0` pentru un vector fără magnitudine și respinge valori nenumerice.
- `KMeansClusterer.Cluster` grupează vectorii utilizatorilor prin distanță euclidiană,
  cu inițializare deterministă farthest-first și limită configurabilă de iterații.
- `TopKSelector.Select` ordonează filmele după similaritatea cosinus, rupe egalitățile
  după titlu/ID și poate exclude filme deja evaluate. Cere `K >= 0`.

Complexități: cosine este `O(d)` așteptat pentru vectori rari; K-Means este aproximativ
`O(I × n × k × d)` per iterații, pe lângă inițializarea centrelor; selectorul Top-K
curent sortează candidații și costă `O(m log m)`. Parametrii reprezintă dimensiunile
`d` (genuri), `n` (utilizatori), `k` (clustere), `I` (iterații) și `m` (filme candidate).

Testele unitare sunt în `tests/MovieMind.Application.Tests`. Rulează-le cu:

```sh
dotnet test tests/MovieMind.Application.Tests/MovieMind.Application.Tests.csproj
```

Motorul din `MovieMind.Application/Services/RecommendationService.cs` folosește
similaritatea cosinus dintre profilul pe genuri și vectorii de filme, apoi combină
scorul de conținut (65%) cu media evaluărilor utilizatorilor din același cluster
K-Means (35%) când există vecini relevanți. Filmele deja evaluate sunt excluse.
Un cont fără preferințe și evaluări primește filme ordonate după ratingul catalogului
cu strategia `Popularity`; pe măsură ce adaugă date, primește strategia `ContentBased`
sau `Hybrid`. Răspunsul include explicația fiecărei recomandări și scorurile folosite.

Opțional, poți porni separat proiectul API:

```sh
dotnet run --project MovieMind/MovieMind.API.csproj --launch-profile http
```

Documentul OpenAPI: **http://localhost:5048/openapi/v1.json**.
Interfața Swagger: **http://localhost:5048/swagger**.
Valoarea introdusă pentru un gen este nivelul interesului utilizatorului, nu o evaluare a calității genului. Vectorul este ordonat după numele genului și normalizează nivelul 0–10 în afinitate -1..1 (5 devine neutru 0); dacă nu există preferință explicită, folosește media evaluărilor utilizatorului pentru acel gen. Fără semnal, valoarea este 0.
API-ul separat păstrează și controllerele existente din `Dev` pentru
`/api/movies`, `/api/actors` și `/api/genres`; acestea folosesc aceeași bază de date.
Formularul complet din frontend folosește `/api/films`, care include toate
câmpurile MVP-ului. Controllerele vechi au contracte mai restrânse.
Rădăcina API-ului `/` răspunde cu 404; folosește rutele `/api/films` etc.
Ambele proiecte folosesc același backend din `MovieMind.Hosting` și, implicit,
aceeași bază de date din acest checkout. Interfața folosește propriul server,
deci nu trebuie pornit API-ul separat și nu este necesară configurarea CORS.

## 5. Unde modifici codul

| Director / fișier | Responsabilitate |
| --- | --- |
| `MovieMind.Web/wwwroot/js/site.js` | Interfața, formularul, upload-ul și apelurile API. |
| `MovieMind.Web/wwwroot/css/site.css` | Designul și comportamentul responsive al modalului. |
| `MovieMind.Web/Pages/Index.cshtml` | Punctul de montare al interfeței. |
| `MovieMind.Web/Pages/Shared/_Layout.cshtml` | Layout și încărcarea scripturilor/CSS. |
| `MovieMind.Hosting/FilmDocument.cs` | Contractul JSON, validarea și maparea câmpurilor. |
| `MovieMind.Application/Interfaces` și `Services` | Contractele și serviciile CRUD existente în Dev. |
| `MovieMind/Controllers` | Controllerele CRUD existente, disponibile în API-ul separat. |
| `MovieMind.Hosting/FilmCatalog.cs` | Salvarea filmelor, genurilor și distribuției. |
| `MovieMind.Hosting/MovieMindHosting.cs` | Endpoint-uri, upload și configurarea bazei de date. |
| `MovieMind.Domain/Entities` | Entitățile bazei de date. |
| `MovieMind.Infrastructure/Configurations` | Configurarea EF: coloane, limite și relații. |
| `MovieMind.Infrastructure/Migrations` | Migrațiile SQL Server. |
| `MovieMind.Infrastructure/Migrations/Sqlite` | Migrațiile SQLite. |
| `MovieMind/DatabaseDesignFactories.cs` | Contexte pentru comenzile EF executate din rădăcina proiectului. |
| `tests/test_catalog.py` | Teste HTTP cu baze și imagini temporare. |
| `tests/MovieMind.Application.Tests` | Teste unitare pentru algoritmii APA. |

`MovieMind.Domain/MovieMind.Web` este un scaffold separat, exclus din compilarea
bibliotecii Domain. Interfața folosită efectiv este proiectul `MovieMind.Web`
din rădăcină.

## 6. Verificări înainte de următorul commit

```sh
dotnet build MovieMind.slnx
dotnet test tests/MovieMind.Application.Tests/MovieMind.Application.Tests.csproj
python3 -m unittest discover -s tests -v
git diff --check
git status
```

Ultimele verificări: build fără avertismente/erori și 8 teste de integrare reușite.
Testele folosesc servere, baze SQLite și directoare temporare; nu modifică colecția
locală. Au fost verificate și în Firefox upload-ul, drag-and-drop, decodorul de
rezervă, salvarea tuturor câmpurilor, editarea, ștergerea și persistența după reload.
Pentru zona de upload s-au verificat evenimente reale de pointer pe text, icon,
spațiul liber și în ferestre desktop scurte, tabletă și mobil.

## 7. Dacă schimbi structura bazei de date

Modifică entitățile, configurările EF și, după caz, contractul JSON/formularul.
Generează apoi câte o migrare pentru fiecare provider, folosind un nume nou:

```sh
dotnet ef migrations add NumeSchimbare --context ApplicationDbContext --project MovieMind.Infrastructure --startup-project MovieMind/MovieMind.API.csproj --output-dir Migrations
dotnet ef migrations add NumeSchimbareSqlite --context SqliteApplicationDbContext --project MovieMind.Infrastructure --startup-project MovieMind/MovieMind.API.csproj --output-dir Migrations/Sqlite
```

Comenzile necesită `dotnet-ef` compatibil cu EF Core 10. Verifică migrațiile generate
înainte de aplicare. SQLite le aplică automat în Development; în alte medii:

```sh
dotnet ef database update --context SqliteApplicationDbContext --project MovieMind.Infrastructure --startup-project MovieMind/MovieMind.API.csproj
```

Pentru alt director de date, setează `Storage__DataDirectory` la o cale absolută.
`ConnectionStrings__Sqlite` poate suprascrie conexiunea SQLite. Dacă pornești ambele
host-uri, folosește aceleași setări pentru a vedea aceeași colecție.

### Folosirea SQL Server

SQLite este configurația locală implicită. Pentru SQL Server, setează în terminal:

```sh
export Database__Provider=SqlServer
export ConnectionStrings__DefaultConnection='Server=localhost;Database=MovieMindDb;Trusted_Connection=True;TrustServerCertificate=True'
dotnet ef database update --context ApplicationDbContext --project MovieMind.Infrastructure --startup-project MovieMind/MovieMind.API.csproj
dotnet run --project MovieMind.Web --launch-profile http
```

Adaptează conexiunea și autentificarea la serverul tău; nu salva parole în Git.
Migrațiile SQL Server se aplică explicit, nu automat la pornire. Schimbarea
providerului nu transferă filmele existente din SQLite în SQL Server.

## 8. Backup și ce ajunge în Git

Pentru un backup local complet, oprește aplicația cu Ctrl+C și închide orice
editor al bazei de date, apoi copiază întregul director `App_Data/` într-o locație
de backup. Păstrează baza și posterele împreună.
Exportul JSON din Studio conține metadate și URL-uri, nu fișierele imaginilor.

În Git intră sursele, migrațiile și testele. `App_Data/`, `bin/`, `obj/` și
`__pycache__/` sunt ignorate. Un `git pull` nu aduce și baza de date a altui coleg.

Pentru următoarea sesiune de lucru: citește acest README, verifică `git status`,
pornește `MovieMind.Web`, confirmă ce bază de date folosești și inspectează tabelele
înainte să presupui că lipsește o salvare.
