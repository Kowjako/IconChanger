# IconChanger
Program służy do zamiana ikonek w plikach .exe. Program napisany na platformie WPF.
# Instrukcja
*Wrzucamy plik w pole APP żądaną ikonkę w pole ICON i naciskamy Confirm*
# Realizacja programowa
Program polega na 3 etapach:  
**Etap 1** Pobranie ikonki końcowej realizuje zdarzenie ``Drop`` następnie tworzy na podstawie ścieżki pliku BitmapSource który następnie jest używany do ustawiania w polu ICON które jest elementem Image w WPF  
**Etap 2** Pobranie ikonki z pliku o rozszerzeniu .exe odbywa się za pomocą biblioteki ``TAFactory.IconPack.dll`` kod zródłowy tej biblioteki można znaleźć tutaj [IconPack](https://www.codeproject.com/Articles/32617/Extracting-Icons-from-EXE-DLL-and-Icon-Manipulatio) pobranie ikonki odbywa się następująco:
``` c#
List<Icon> icons = IconHelper.ExtractAllIcons(iconPath);
Icon smallIcon = IconHelper.GetBestFitIcon(icons[0], new System.Drawing.Size(64, 64));
FileStream fs = File.Create(path);
smallIcon.Save(fs);
fs.Close();
```
**Etap 3** Dołączenie ikonki do zasobów pliku o rozszerzeniu .exe odbywa się za pomocą klasy IconInjector kod źrodłowy był wzięty z forum CyberForum [IconInejctor](https://www.cyberforum.ru/post13136359.html) oraz przepisany w języku C# po dołączeniu tej klasy zamiana ikonki odbywa się za pomocą polecenia:
``` c#
IconInjector.InjectIcon(appPath, endIconPath);
```

# Screenshot
![Screenshot_1](https://user-images.githubusercontent.com/19534189/106033588-309e2000-60d2-11eb-8ab5-cee3a6792b66.png)
