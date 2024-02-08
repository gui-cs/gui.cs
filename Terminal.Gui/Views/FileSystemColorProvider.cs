// This code is adapted from https://github.com/devblackops/Terminal-Icons (which also uses the MIT license).

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace Terminal.Gui;

/// <summary>Provides a way to get the color of a file or directory.</summary>
public class FileSystemColorProvider {
    /// <summary>Mapping of file extension to color.</summary>
    public Dictionary<string, Color> ExtensionToColor = new () {
        { ".7z", StringToColor ("#DAA520") },
        { ".bz", StringToColor ("#DAA520") },
        { ".tar", StringToColor ("#DAA520") },
        { ".zip", StringToColor ("#DAA520") },
        { ".gz", StringToColor ("#DAA520") },
        { ".xz", StringToColor ("#DAA520") },
        { ".br", StringToColor ("#DAA520") },
        { ".bzip2", StringToColor ("#DAA520") },
        { ".gzip", StringToColor ("#DAA520") },
        { ".brotli", StringToColor ("#DAA520") },
        { ".rar", StringToColor ("#DAA520") },
        { ".tgz", StringToColor ("#DAA520") },
        { ".bat", StringToColor ("#008000") },
        { ".cmd", StringToColor ("#008000") },
        { ".exe", StringToColor ("#00FA9A") },
        { ".pl", StringToColor ("#8A2BE2") },
        { ".sh", StringToColor ("#FF4500") },
        { ".msi", StringToColor ("#FFC77A") },
        { ".msix", StringToColor ("#FFC77A") },
        { ".msixbundle", StringToColor ("#FFC77A") },
        { ".appx", StringToColor ("#FFC77A") },
        { ".AppxBundle", StringToColor ("#FFC77A") },
        { ".deb", StringToColor ("#FFC77A") },
        { ".rpm", StringToColor ("#FFC77A") },
        { ".ps1", StringToColor ("#00BFFF") },
        { ".psm1", StringToColor ("#00BFFF") },
        { ".psd1", StringToColor ("#00BFFF") },
        { ".ps1xml", StringToColor ("#00BFFF") },
        { ".psc1", StringToColor ("#00BFFF") },
        { ".pssc", StringToColor ("#00BFFF") },
        { ".js", StringToColor ("#F0E68C") },
        { ".esx", StringToColor ("#F0E68C") },
        { ".mjs", StringToColor ("#F0E68C") },
        { ".java", StringToColor ("#F89820") },
        { ".jar", StringToColor ("#F89820") },
        { ".gradle", StringToColor ("#39D52D") },
        { ".py", StringToColor ("#4B8BBE") },
        { ".ipynb", StringToColor ("#4B8BBE") },
        { ".jsx", StringToColor ("#20B2AA") },
        { ".tsx", StringToColor ("#20B2AA") },
        { ".ts", StringToColor ("#F0E68C") },
        { ".dll", StringToColor ("#87CEEB") },
        { ".clixml", StringToColor ("#00BFFF") },
        { ".csv", StringToColor ("#9ACD32") },
        { ".tsv", StringToColor ("#9ACD32") },
        { ".ini", StringToColor ("#6495ED") },
        { ".dlc", StringToColor ("#6495ED") },
        { ".config", StringToColor ("#6495ED") },
        { ".conf", StringToColor ("#6495ED") },
        { ".properties", StringToColor ("#6495ED") },
        { ".prop", StringToColor ("#6495ED") },
        { ".settings", StringToColor ("#6495ED") },
        { ".option", StringToColor ("#6495ED") },
        { ".reg", StringToColor ("#6495ED") },
        { ".props", StringToColor ("#6495ED") },
        { ".toml", StringToColor ("#6495ED") },
        { ".prefs", StringToColor ("#6495ED") },
        { ".sln.dotsettings", StringToColor ("#6495ED") },
        { ".sln.dotsettings.user", StringToColor ("#6495ED") },
        { ".cfg", StringToColor ("#6495ED") },
        { ".c", StringToColor ("#A9A9A9") },
        { ".cpp", StringToColor ("#A9A9A9") },
        { ".go", StringToColor ("#20B2AA") },
        { ".php", StringToColor ("#6A5ACD") },
        { ".csproj", StringToColor ("#EE82EE") },
        { ".ruleset", StringToColor ("#EE82EE") },
        { ".sln", StringToColor ("#EE82EE") },
        { ".slnf", StringToColor ("#EE82EE") },
        { ".suo", StringToColor ("#EE82EE") },
        { ".vb", StringToColor ("#EE82EE") },
        { ".vbs", StringToColor ("#EE82EE") },
        { ".vcxitems", StringToColor ("#EE82EE") },
        { ".vcxitems.filters", StringToColor ("#EE82EE") },
        { ".vcxproj", StringToColor ("#EE82EE") },
        { ".vsxproj.filters", StringToColor ("#EE82EE") },
        { ".cs", StringToColor ("#7B68EE") },
        { ".csx", StringToColor ("#7B68EE") },
        { ".hs", StringToColor ("#9932CC") },
        { ".xaml", StringToColor ("#87CEFA") },
        { ".rs", StringToColor ("#FF4500") },
        { ".pdb", StringToColor ("#FFD700") },
        { ".sql", StringToColor ("#FFD700") },
        { ".pks", StringToColor ("#FFD700") },
        { ".pkb", StringToColor ("#FFD700") },
        { ".accdb", StringToColor ("#FFD700") },
        { ".mdb", StringToColor ("#FFD700") },
        { ".sqlite", StringToColor ("#FFD700") },
        { ".pgsql", StringToColor ("#FFD700") },
        { ".postgres", StringToColor ("#FFD700") },
        { ".psql", StringToColor ("#FFD700") },
        { ".patch", StringToColor ("#FF4500") },
        { ".user", StringToColor ("#00BFFF") },
        { ".code-workspace", StringToColor ("#00BFFF") },
        { ".log", StringToColor ("#F0E68C") },
        { ".txt", StringToColor ("#00CED1") },
        { ".srt", StringToColor ("#00CED1") },
        { ".lrc", StringToColor ("#00CED1") },
        { ".ass", StringToColor ("#C50000") },
        { ".html", StringToColor ("#CD5C5C") },
        { ".htm", StringToColor ("#CD5C5C") },
        { ".xhtml", StringToColor ("#CD5C5C") },
        { ".html_vm", StringToColor ("#CD5C5C") },
        { ".asp", StringToColor ("#CD5C5C") },
        { ".css", StringToColor ("#87CEFA") },
        { ".sass", StringToColor ("#FF00FF") },
        { ".scss", StringToColor ("#FF00FF") },
        { ".less", StringToColor ("#6B8E23") },
        { ".md", StringToColor ("#00BFFF") },
        { ".markdown", StringToColor ("#00BFFF") },
        { ".rst", StringToColor ("#00BFFF") },
        { ".hbs", StringToColor ("#E37933") },
        { ".json", StringToColor ("#FFD700") },
        { ".tsbuildinfo", StringToColor ("#FFD700") },
        { ".yml", StringToColor ("#FF6347") },
        { ".yaml", StringToColor ("#FF6347") },
        { ".lua", StringToColor ("#87CEFA") },
        { ".clj", StringToColor ("#00FF7F") },
        { ".cljs", StringToColor ("#00FF7F") },
        { ".cljc", StringToColor ("#00FF7F") },
        { ".groovy", StringToColor ("#87CEFA") },
        { ".vue", StringToColor ("#20B2AA") },
        { ".dart", StringToColor ("#4682B4") },
        { ".ex", StringToColor ("#8B4513") },
        { ".exs", StringToColor ("#8B4513") },
        { ".eex", StringToColor ("#8B4513") },
        { ".leex", StringToColor ("#8B4513") },
        { ".erl", StringToColor ("#FF6347") },
        { ".elm", StringToColor ("#9932CC") },
        { ".applescript", StringToColor ("#4682B4") },
        { ".xml", StringToColor ("#98FB98") },
        { ".plist", StringToColor ("#98FB98") },
        { ".xsd", StringToColor ("#98FB98") },
        { ".dtd", StringToColor ("#98FB98") },
        { ".xsl", StringToColor ("#98FB98") },
        { ".xslt", StringToColor ("#98FB98") },
        { ".resx", StringToColor ("#98FB98") },
        { ".iml", StringToColor ("#98FB98") },
        { ".xquery", StringToColor ("#98FB98") },
        { ".tmLanguage", StringToColor ("#98FB98") },
        { ".manifest", StringToColor ("#98FB98") },
        { ".project", StringToColor ("#98FB98") },
        { ".chm", StringToColor ("#87CEEB") },
        { ".pdf", StringToColor ("#CD5C5C") },
        { ".xls", StringToColor ("#9ACD32") },
        { ".xlsx", StringToColor ("#9ACD32") },
        { ".pptx", StringToColor ("#DC143C") },
        { ".ppt", StringToColor ("#DC143C") },
        { ".pptm", StringToColor ("#DC143C") },
        { ".potx", StringToColor ("#DC143C") },
        { ".potm", StringToColor ("#DC143C") },
        { ".ppsx", StringToColor ("#DC143C") },
        { ".ppsm", StringToColor ("#DC143C") },
        { ".pps", StringToColor ("#DC143C") },
        { ".ppam", StringToColor ("#DC143C") },
        { ".ppa", StringToColor ("#DC143C") },
        { ".doc", StringToColor ("#00BFFF") },
        { ".docx", StringToColor ("#00BFFF") },
        { ".rtf", StringToColor ("#00BFFF") },
        { ".mp3", StringToColor ("#DB7093") },
        { ".flac", StringToColor ("#DB7093") },
        { ".m4a", StringToColor ("#DB7093") },
        { ".wma", StringToColor ("#DB7093") },
        { ".aiff", StringToColor ("#DB7093") },
        { ".wav", StringToColor ("#DB7093") },
        { ".aac", StringToColor ("#DB7093") },
        { ".opus", StringToColor ("#DB7093") },
        { ".png", StringToColor ("#20B2AA") },
        { ".jpeg", StringToColor ("#20B2AA") },
        { ".jpg", StringToColor ("#20B2AA") },
        { ".gif", StringToColor ("#20B2AA") },
        { ".ico", StringToColor ("#20B2AA") },
        { ".tif", StringToColor ("#20B2AA") },
        { ".tiff", StringToColor ("#20B2AA") },
        { ".psd", StringToColor ("#20B2AA") },
        { ".psb", StringToColor ("#20B2AA") },
        { ".ami", StringToColor ("#20B2AA") },
        { ".apx", StringToColor ("#20B2AA") },
        { ".bmp", StringToColor ("#20B2AA") },
        { ".bpg", StringToColor ("#20B2AA") },
        { ".brk", StringToColor ("#20B2AA") },
        { ".cur", StringToColor ("#20B2AA") },
        { ".dds", StringToColor ("#20B2AA") },
        { ".dng", StringToColor ("#20B2AA") },
        { ".eps", StringToColor ("#20B2AA") },
        { ".exr", StringToColor ("#20B2AA") },
        { ".fpx", StringToColor ("#20B2AA") },
        { ".gbr", StringToColor ("#20B2AA") },
        { ".jbig2", StringToColor ("#20B2AA") },
        { ".jb2", StringToColor ("#20B2AA") },
        { ".jng", StringToColor ("#20B2AA") },
        { ".jxr", StringToColor ("#20B2AA") },
        { ".pbm", StringToColor ("#20B2AA") },
        { ".pgf", StringToColor ("#20B2AA") },
        { ".pic", StringToColor ("#20B2AA") },
        { ".raw", StringToColor ("#20B2AA") },
        { ".webp", StringToColor ("#20B2AA") },
        { ".svg", StringToColor ("#F4A460") },
        { ".webm", StringToColor ("#FFA500") },
        { ".mkv", StringToColor ("#FFA500") },
        { ".flv", StringToColor ("#FFA500") },
        { ".vob", StringToColor ("#FFA500") },
        { ".ogv", StringToColor ("#FFA500") },
        { ".ogg", StringToColor ("#FFA500") },
        { ".gifv", StringToColor ("#FFA500") },
        { ".avi", StringToColor ("#FFA500") },
        { ".mov", StringToColor ("#FFA500") },
        { ".qt", StringToColor ("#FFA500") },
        { ".wmv", StringToColor ("#FFA500") },
        { ".yuv", StringToColor ("#FFA500") },
        { ".rm", StringToColor ("#FFA500") },
        { ".rmvb", StringToColor ("#FFA500") },
        { ".mp4", StringToColor ("#FFA500") },
        { ".mpg", StringToColor ("#FFA500") },
        { ".mp2", StringToColor ("#FFA500") },
        { ".mpeg", StringToColor ("#FFA500") },
        { ".mpe", StringToColor ("#FFA500") },
        { ".mpv", StringToColor ("#FFA500") },
        { ".m2v", StringToColor ("#FFA500") },
        { ".ics", StringToColor ("#00CED1") },
        { ".cer", StringToColor ("#FF6347") },
        { ".cert", StringToColor ("#FF6347") },
        { ".crt", StringToColor ("#FF6347") },
        { ".pfx", StringToColor ("#FF6347") },
        { ".pem", StringToColor ("#66CDAA") },
        { ".pub", StringToColor ("#66CDAA") },
        { ".key", StringToColor ("#66CDAA") },
        { ".asc", StringToColor ("#66CDAA") },
        { ".gpg", StringToColor ("#66CDAA") },
        { ".woff", StringToColor ("#DC143C") },
        { ".woff2", StringToColor ("#DC143C") },
        { ".ttf", StringToColor ("#DC143C") },
        { ".eot", StringToColor ("#DC143C") },
        { ".suit", StringToColor ("#DC143C") },
        { ".otf", StringToColor ("#DC143C") },
        { ".bmap", StringToColor ("#DC143C") },
        { ".fnt", StringToColor ("#DC143C") },
        { ".odttf", StringToColor ("#DC143C") },
        { ".ttc", StringToColor ("#DC143C") },
        { ".font", StringToColor ("#DC143C") },
        { ".fonts", StringToColor ("#DC143C") },
        { ".sui", StringToColor ("#DC143C") },
        { ".ntf", StringToColor ("#DC143C") },
        { ".mrg", StringToColor ("#DC143C") },
        { ".rb", StringToColor ("#FF0000") },
        { ".erb", StringToColor ("#FF0000") },
        { ".gemfile", StringToColor ("#FF0000") },
        { "Rakefile", StringToColor ("#FF0000") },
        { ".fs", StringToColor ("#00BFFF") },
        { ".fsx", StringToColor ("#00BFFF") },
        { ".fsi", StringToColor ("#00BFFF") },
        { ".fsproj", StringToColor ("#00BFFF") },
        { ".dockerignore", StringToColor ("#4682B4") },
        { ".dockerfile", StringToColor ("#4682B4") },
        { ".vscodeignore", StringToColor ("#6495ED") },
        { ".vsixmanifest", StringToColor ("#6495ED") },
        { ".vsix", StringToColor ("#6495ED") },
        { ".code-workplace", StringToColor ("#6495ED") },
        { ".sublime-project", StringToColor ("#F4A460") },
        { ".sublime-workspace", StringToColor ("#F4A460") },
        { ".lock", StringToColor ("#DAA520") },
        { ".tf", StringToColor ("#948EEC") },
        { ".tfvars", StringToColor ("#948EEC") },
        { ".auto.tfvars", StringToColor ("#948EEC") },
        { ".bicep", StringToColor ("#00BFFF") },
        { ".vmdk", StringToColor ("#E1E3E6") },
        { ".vhd", StringToColor ("#E1E3E6") },
        { ".vhdx", StringToColor ("#E1E3E6") },
        { ".img", StringToColor ("#E1E3E6") },
        { ".iso", StringToColor ("#E1E3E6") },
        { ".R", StringToColor ("#276DC3") },
        { ".Rmd", StringToColor ("#276DC3") },
        { ".Rproj", StringToColor ("#276DC3") },
        { ".jl", StringToColor ("#9259a3") },
        { ".vim", StringToColor ("#019833") },
        { ".pp", StringToColor ("#FFA61A") },
        { ".epp", StringToColor ("#FFA61A") },
        { ".scala", StringToColor ("#DE3423") },
        { ".sc", StringToColor ("#DE3423") },
        { ".iLogicVb", StringToColor ("#A63B22") }
    };

    /// <summary>Mapping of file/dir name to color.</summary>
    public Dictionary<string, Color> FilenameToColor { get; set; } = new () {
        { "docs", StringToColor ("#00BFFF") },
        { "documents", StringToColor ("#00BFFF") },
        { "desktop", StringToColor ("#00FBFF") },
        { "benchmark", StringToColor ("#F08519") },
        { "demo", StringToColor ("#5F3EC3") },
        { "samples", StringToColor ("#5F3EC3") },
        { "contacts", StringToColor ("#00FBFF") },
        { "apps", StringToColor ("#FF143C") },
        { "applications", StringToColor ("#FF143C") },
        { "artifacts", StringToColor ("#D49653") },
        { "shortcuts", StringToColor ("#FF143C") },
        { "links", StringToColor ("#FF143C") },
        { "fonts", StringToColor ("#DC143C") },
        { "images", StringToColor ("#9ACD32") },
        { "photos", StringToColor ("#9ACD32") },
        { "pictures", StringToColor ("#9ACD32") },
        { "videos", StringToColor ("#FFA500") },
        { "movies", StringToColor ("#FFA500") },
        { "media", StringToColor ("#D3D3D3") },
        { "music", StringToColor ("#DB7093") },
        { "songs", StringToColor ("#DB7093") },
        { "onedrive", StringToColor ("#D3D3D3") },
        { "downloads", StringToColor ("#D3D3D3") },
        { "src", StringToColor ("#00FF7F") },
        { "development", StringToColor ("#00FF7F") },
        { "projects", StringToColor ("#00FF7F") },
        { "bin", StringToColor ("#00FFF7") },
        { "tests", StringToColor ("#87CEEB") },
        { "windows", StringToColor ("#00A8E8") },
        { "users", StringToColor ("#F4F4F4") },
        { "favorites", StringToColor ("#F7D72C") },
        { "output", StringToColor ("#00FF7F") },
        { ".config", StringToColor ("#87CEAF") },
        { ".cache", StringToColor ("#87ECAF") },
        { ".vscode", StringToColor ("#87CEFA") },
        { ".vscode-insiders", StringToColor ("#24BFA5") },
        { ".git", StringToColor ("#FF4500") },
        { ".github", StringToColor ("#C0C0C0") },
        { "github", StringToColor ("#C0C0C0") },
        { "node_modules", StringToColor ("#6B8E23") },
        { ".terraform", StringToColor ("#948EEC") },
        { ".azure", StringToColor ("#00BFFF") },
        { ".aws", StringToColor ("#EC912D") },
        { ".kube", StringToColor ("#326DE6") },
        { ".docker", StringToColor ("#2391E6") },
        { ".gitattributes", StringToColor ("#FF4500") },
        { ".gitconfig", StringToColor ("#FF4500") },
        { ".gitignore", StringToColor ("#FF4500") },
        { ".gitmodules", StringToColor ("#FF4500") },
        { ".gitkeep", StringToColor ("#FF4500") },
        { "git-history", StringToColor ("#FF4500") },
        { "LICENSE", StringToColor ("#CD5C5C") },
        { "LICENSE.md", StringToColor ("#CD5C5C") },
        { "LICENSE.txt", StringToColor ("#CD5C5C") },
        { "CHANGELOG.md", StringToColor ("#98FB98") },
        { "CHANGELOG.txt", StringToColor ("#98FB98") },
        { "CHANGELOG", StringToColor ("#98FB98") },
        { "README.md", StringToColor ("#00FFFF") },
        { "README.txt", StringToColor ("#00FFFF") },
        { "README", StringToColor ("#00FFFF") },
        { ".DS_Store", StringToColor ("#696969") },
        { ".tsbuildinfo", StringToColor ("#F4A460") },
        { ".jscsrc", StringToColor ("#F4A460") },
        { ".jshintrc", StringToColor ("#F4A460") },
        { "tsconfig.json", StringToColor ("#F4A460") },
        { "tslint.json", StringToColor ("#F4A460") },
        { "composer.lock", StringToColor ("#F4A460") },
        { ".jsbeautifyrc", StringToColor ("#F4A460") },
        { ".esformatter", StringToColor ("#F4A460") },
        { "cdp.pid", StringToColor ("#F4A460") },
        { ".htaccess", StringToColor ("#9ACD32") },
        { ".jshintignore", StringToColor ("#87CEEB") },
        { ".buildignore", StringToColor ("#87CEEB") },
        { ".mrconfig", StringToColor ("#87CEEB") },
        { ".yardopts", StringToColor ("#87CEEB") },
        { "manifest.mf", StringToColor ("#87CEEB") },
        { ".clang-format", StringToColor ("#87CEEB") },
        { ".clang-tidy", StringToColor ("#87CEEB") },
        { "favicon.ico", StringToColor ("#FFD700") },
        { ".travis.yml", StringToColor ("#FFE4B5") },
        { ".gitlab-ci.yml", StringToColor ("#FF4500") },
        { ".jenkinsfile", StringToColor ("#6495ED") },
        { "bitbucket-pipelines.yml", StringToColor ("#87CEFA") },
        { "bitbucket-pipelines.yaml", StringToColor ("#87CEFA") },
        { ".azure-pipelines.yml", StringToColor ("#00BFFF") },
        { "firebase.json", StringToColor ("#FFA500") },
        { ".firebaserc", StringToColor ("#FFA500") },
        { ".bowerrc", StringToColor ("#CD5C5C") },
        { "bower.json", StringToColor ("#CD5C5C") },
        { "code_of_conduct.md", StringToColor ("#FFFFE0") },
        { "code_of_conduct.txt", StringToColor ("#FFFFE0") },
        { "Dockerfile", StringToColor ("#4682B4") },
        { "docker-compose.yml", StringToColor ("#4682B4") },
        { "docker-compose.yaml", StringToColor ("#4682B4") },
        { "docker-compose.dev.yml", StringToColor ("#4682B4") },
        { "docker-compose.local.yml", StringToColor ("#4682B4") },
        { "docker-compose.ci.yml", StringToColor ("#4682B4") },
        { "docker-compose.override.yml", StringToColor ("#4682B4") },
        { "docker-compose.staging.yml", StringToColor ("#4682B4") },
        { "docker-compose.prod.yml", StringToColor ("#4682B4") },
        { "docker-compose.production.yml", StringToColor ("#4682B4") },
        { "docker-compose.test.yml", StringToColor ("#4682B4") },
        { "vue.config.js", StringToColor ("#778899") },
        { "vue.config.ts", StringToColor ("#778899") },
        { "gulpfile.js", StringToColor ("#CD5C5C") },
        { "gulpfile.ts", StringToColor ("#CD5C5C") },
        { "gulpfile.babel.js", StringToColor ("#CD5C5C") },
        { "gruntfile.js", StringToColor ("#CD5C5C") },
        { "package.json", StringToColor ("#6B8E23") },
        { "package-lock.json", StringToColor ("#6B8E23") },
        { ".nvmrc", StringToColor ("#6B8E23") },
        { ".esmrc", StringToColor ("#6B8E23") },
        { ".nmpignore", StringToColor ("#00BFFF") },
        { ".npmrc", StringToColor ("#00BFFF") },
        { "authors", StringToColor ("#FF6347") },
        { "authors.md", StringToColor ("#FF6347") },
        { "authors.txt", StringToColor ("#FF6347") },
        { ".terraform.lock.hcl", StringToColor ("#948EEC") },
        { "gradlew", StringToColor ("#39D52D") }
    };

    /// <summary>Gets the color to use.</summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public Color? GetColor (IFileSystemInfo file) {
        if (FilenameToColor.TryGetValue (file.Name, out Color nameColor)) {
            return nameColor;
        }

        if (ExtensionToColor.TryGetValue (file.Extension, out Color extColor)) {
            return extColor;
        }

        return null;
    }

    private static Color StringToColor (string str) {
        if (!Color.TryParse (str, out Color? c)) {
            ThrowFormatException (str);
        }

        return c.Value;

        [DoesNotReturn]
        static void ThrowFormatException (string s) {
            throw new FormatException ($"Failed to parse Color from {s}");
        }
    }
}
