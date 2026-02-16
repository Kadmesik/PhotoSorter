#  PhotoSorter

**PhotoSorter** to lekka i szybka aplikacja desktopowa (WPF) służąca do błyskawicznego sortowania dużych kolekcji zdjęć przy użyciu skrótów klawiszowych.

Zamiast przeciągać pliki myszką, definiujesz nazwy folderów i "strzelasz" klawiszami 1-8. Idealne narzędzie do porządkowania zdjęć z wakacji, memów czy dokumentów.

##  Główne Funkcje

* **Szybkość:** Sortowanie odbywa się wyłącznie za pomocą klawiatury.
* **Menu Konfiguracyjne:** Przed rozpoczęciem pracy wybierasz folder źródłowy i nadajesz własne nazwy folderom docelowym (np. "Wakacje", "Praca", "Do Obrobienia").
* **Bezpieczeństwo:** Możliwość cofnięcia ostatnich 5 operacji (Ctrl+Z) - w tym przenoszenia i usuwania. Jeżeli pomyliłeś się więcej razy foldery tworzą się wenątrz folderu ze zdjęciami, dalej masz łatwy dostęp do swoich zdjęć.
* **Wsparcie formatów:** Obsługuje JPG, PNG, GIF.
* **Ciemny Motyw:** Nowoczesny interfejs UI przyjazny dla oczu.

##  Sterowanie

| Klawisz | Akcja | Opis |
| :--- | :--- | :--- |
| **1 - 8** | **Przenieś** | Przenosi zdjęcie do przypisanego folderu (np. Folder1). |
| **9** | **Pomiń** | Zostawia zdjęcie w obecnym folderze i przechodzi do następnego. |
| **0** | **Usuń** | Przenosi zdjęcie do folderu "Kosz". |
| **Ctrl + Z** | **Cofnij** | Cofa ostatnią akcję (do 5 kroków wstecz). |

##  Technologie

* **Język:** C# (.NET 9)
* **Framework:** WPF (Windows Presentation Foundation)
* **IDE:** Visual Studio 2022

##  Jak uruchomić?

1.  Sklonuj repozytorium:
    ```bash
    git clone https://github.com/Kadmesik/PhotoSorter.git
    ```
2.  Otwórz plik `PhotoSorter.sln` w Visual Studio.
3.  Uruchom projekt (F5).

#### lub w ten spób: