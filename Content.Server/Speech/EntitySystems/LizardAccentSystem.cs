using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class LizardAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");
    // stalker local
    private static readonly Regex RegexRusS = new("[сзц]+");
    private static readonly Regex RegexRusCapsS = new("[СЗЦ]+");
    private static readonly Regex RegexRusSh = new("[шж]+");
    private static readonly Regex RegexRusCapsSh = new("[ШЖ]+");
    private static readonly Regex RegexRusCh = new("ч+");
    private static readonly Regex RegexRusCapsCh = new("Ч+");
    private static readonly Regex RegexRusSch = new("щ+");
    private static readonly Regex RegexRusCapsSch = new("Щ+");
    //

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, LizardAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // hissss
        message = RegexLowerS.Replace(message, "sss");
        // hiSSS
        message = RegexUpperS.Replace(message, "SSS");
        // ekssit
        message = RegexInternalX.Replace(message, "$1kss");
        // ecks
        message = RegexLowerEndX.Replace(message, "ecks$1");
        // eckS
        message = RegexUpperEndX.Replace(message, "ECKS$1");

        // stalker local
        message = RegexRusS.Replace(message, _ => _random.Pick(new[] { "сс", "ссс" }));
        message = RegexRusCapsS.Replace(message, _ => _random.Pick(new[] { "СС", "ССС" }));
        
        message = RegexRusSh.Replace(message, _ => _random.Pick(new[] { "шш", "шшш" }));
        message = RegexRusCapsSh.Replace(message, _ => _random.Pick(new[] { "ШШ", "ШШШ" }));
        
        message = RegexRusCh.Replace(message, _ => _random.Pick(new[] { "щщ", "щщщ" }));
        message = RegexRusCapsCh.Replace(message, _ => _random.Pick(new[] { "ЩЩ", "ЩЩЩ" }));
        
        message = RegexRusSch.Replace(message, _ => _random.Pick(new[] { "щщ", "щщщ" }));
        message = RegexRusCapsSch.Replace(message, _ => _random.Pick(new[] { "ЩЩ", "ЩЩЩ" }));
        //

        args.Message = message;
    }
}