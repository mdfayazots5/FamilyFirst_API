using System.Text.RegularExpressions;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IRegexRuleProvider
{
    Regex? Find(string method, string field);
}
