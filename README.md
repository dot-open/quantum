![repo-banner](./repo-banner.png)

# Quantum - A simple downloader

Quantum is a simple downloader written in C#.

**Still in development, some features may be unstable.**

## Download

Get the latest binaries from [Releases Page](https://github.com/dot-open/quantum/releases).

## Features

Adding, deleting, pausing, resuming and saving download tasks, customizing thread count and user agent.

More Features can be brought to quantum by installing plugins.

## Language

We now support multiple languages. If your language is not in the list, please modify these files:

```diff
+ quantum/Resources/Language/中文.xaml
```

quantum/Views/Container.xaml.cs

```diff
public class LanguageManager
{
  public static List<string> GetAllLang = new List<string>
  {
    "English.xaml", 
+   "中文.xaml"
  };
  ...
}
```

You can add your language to our repo via Pull Requests.
