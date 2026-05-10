# Sistemskoprvideo
## Tema projekta
Projekat je urađen u okviru predmeta Sistemsko programiranje.
Tema projekta je serverska konzolna aplikacija u programskom jeziku C# koja prima zahteve od klijenata i vrši konverziju slike iz RGB formata u crno-beli format, odnosno u nijanse sive.
Primer poziva servera:
text
http://localhost:5050/test.jpg

Korišćene tehnologije
U projektu su korišćeni:
C#
.NET 8.0
Console App
HttpListener
System.Threading
System.Drawing.Common

Opis rada aplikacije

Aplikacija se pokreće kao konzolni program. Nakon pokretanja, server sluša zahteve na adresi:
http://localhost:5050/

Kada korisnik u browser-u pošalje zahtev, na primer:
http://localhost:5050/test.jpg
server iz URL-a uzima naziv fajla test.jpg i traži ga u root folderu.
Ako slika postoji, server je konvertuje u crno-belu sliku i vraća rezultat browser-u. Ako slika ne postoji, vraća se greška 404 Not Found.
Aplikacija podržava .jpg, .jpeg i .png slike.

Pokretanje projekta
Preuzeti ili klonirati repozitorijum.
Otvoriti projekat u Visual Studio okruženju.
Pokrenuti projekat kao Console App.
U root folder ubaciti sliku, na primer test.jpg ili test1.png.
U browser-u otvoriti jedan od sledećih URL-ova:
http://localhost:5050/test.jpg
ili
http://localhost:5050/test1.png
Ako je server pokrenut i slika postoji u root folderu, browser prikazuje crno-belu verziju slike.

Struktura projekta
Projekat je podeljen na više klasa:
Program.cs - pokretanje aplikacije i servera
ImageServer.cs - glavna logika servera
RequestQueue.cs - zajednički red zahteva
ImageCache.cs - keširanje obrađenih slika
ImageProcessor.cs - konverzija slike u crno-belu
Logger.cs - logovanje rada sistema

Osnovni tok rada aplikacije je:
Browser šalje zahtev
        ↓
Server prima zahtev
        ↓
Zahtev se ubacuje u red
        ↓
Worker nit preuzima zahtev
        ↓
Proverava se cache
        ↓
Slika se obrađuje ako nije u cache-u
        ↓
Rezultat se vraća browser-u
Konkurentna obrada zahteva
U projektu je prijem zahteva razdvojen od njihove obrade.
Server ima listener nit koja prima zahteve i ubacuje ih u red. Obradu zahteva vrše worker niti. Broj worker niti je kontrolisan i u ovom projektu iznosi 4.
To znači da server može da primi više zahteva, dok se obrada izvršava konkurentno.
Ako stigne više zahteva nego što trenutno ima slobodnih worker niti, zahtevi čekaju u redu.

Sinhronizacija
Za sinhronizaciju između niti korišćeni su mehanizmi iz System.Threading prostora imena.
Korišćeno je:
Thread
lock
Monitor.Wait
Monitor.Pulse
Monitor.PulseAll
lock se koristi za zaštitu deljenih resursa, kao što su red zahteva, cache memorija i log fajl.
Monitor.Wait se koristi kada worker nit nema zahtev za obradu i treba da čeka.
Monitor.Pulse se koristi kada se u red doda novi zahtev i treba probuditi jednu worker nit.
Cache i vremensko isticanje
U projektu je implementiran cache koji čuva već obrađene slike.
Ako korisnik ponovo zatraži istu sliku, a rezultat još nije istekao, server ne vrši ponovnu konverziju, već vraća rezultat iz cache-a.
Cache koristi vremensko isticanje. U ovom projektu rezultat u cache-u važi 60 sekundi.
Primer rada cache-a:
Cache miss - konverzija slike: test.jpg
Rezultat dodat u cache: test.jpg
Cache hit - rezultat preuzet iz cache-a: test.jpg
Cache expired - rezultat je istekao: test.jpg

Cache stampede zaštita
U projektu je rešena situacija kada više klijenata istovremeno zatraži istu sliku koja nije u cache-u.
U tom slučaju samo jedna nit vrši konverziju slike, dok ostale niti čekaju da rezultat bude spreman.
Kada prva nit završi obradu i upiše rezultat u cache, ostale niti preuzimaju gotov rezultat iz cache-a.
Na taj način se sprečava nepotrebno višestruko obrađivanje iste slike.

Logovanje
Aplikacija vodi evidenciju o radu sistema.
Log poruke se ispisuju u konzoli i upisuju u fajl:
log.txt
Logovanje je realizovano na thread-safe način pomoću lock, jer više worker niti može istovremeno da upisuje poruke.

Obrada grešaka
U projektu su obrađene osnovne greške:
ako korisnik ne unese naziv slike, vraća se 400 Bad Request
ako slika ne postoji u root folderu, vraća se 404 Not Found
ako dođe do greške tokom obrade, vraća se 500 Internal Server Error

Primer zahteva za sliku koja ne postoji:
http://localhost:5050/nepostoji.jpg
Server tada vraća poruku:
404 Not Found - slika ne postoji.

Testiranje
Projekat je testiran kroz browser i PowerShell.
Testirano je:
pokretanje servera
konverzija .jpg slike
konverzija .png slike
cache hit
cache expired
više paralelnih zahteva
cache stampede zaštita
greška kada slika ne postoji

Za testiranje više paralelnih zahteva korišćen je PowerShell:
1..10 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($i)
        Invoke-WebRequest -Uri "http://localhost:5050/test.jpg" -OutFile "$env:TEMP\test_$i.jpg"
    } -ArgumentList $_
}

Get-Job | Wait-Job
Get-Job | Receive-Job
Get-Job | Remove-Job

U logu se vidi da više worker niti prima zahteve, ali da se konverzija iste slike izvršava samo jednom.


Ovaj projekat prikazuje primenu konkurentnog programiranja u C# jeziku kroz serversku aplikaciju koja obrađuje slike.
Kroz projekat su korišćeni red zahteva, worker niti, sinhronizacija, cache memorija, cache stampede zaštita, logovanje i obrada grešaka.
Aplikacija ispunjava zahteve zadatka jer podržava istovremeni rad sa više zahteva, koristi deljene resurse na thread-safe način i omogućava ponovno korišćenje prethodno obrađenih rezultata kroz cache.
