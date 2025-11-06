using NHyphenator;
using NHyphenator.Loaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MapLib.Render.Rewriting;

public class LabelHyphenator
{
    // Test implementation
    // TODO: implement fully

    private Hyphenator _hyphenator;

    public LabelHyphenator(string culturePrefix = "en-gb")
    {
        //_culturePrefix = culturePrefix;

        string patternsFilePath = Path.GetFullPath(
            $"Data/hyphenation/hyph-{culturePrefix}.pat.txt");
        string? exceptionsFilePath = Path.GetFullPath(
            $"Data/hyphenation/hyph-{culturePrefix}.hyp.txt");

        // Pattern file must exist. Exceptions file is optional.
        if (!File.Exists(patternsFilePath))
            throw new ApplicationException(
                "Hyphenation patterns file not found: " + patternsFilePath);
        if (!File.Exists(exceptionsFilePath))
            exceptionsFilePath = null;

        var loader = new FilePatternsLoader(patternsFilePath, exceptionsFilePath);
        _hyphenator = new Hyphenator(loader, "|");
        
    }

    public string Hyphenate(string input)
    {
        //var result = hypenator.HyphenateText(input);
        //throw new NotImplementedException();
        return _hyphenator.HyphenateText(input);
    }
}
