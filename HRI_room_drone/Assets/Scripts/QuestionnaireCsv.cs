using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class QuestionnaireCsv
{
    private const string Header = "SubmittedAtUtc,Name,EncouragedToCollect,LikedEncouragement,WouldCleanRealRoom,TargetClarity,SpeechClarity,Comfort,Suggestions";

    public static void Append(string participantName, IReadOnlyList<string> answers)
    {
        if (string.IsNullOrWhiteSpace(participantName) || answers.Count != 7)
            throw new ArgumentException("A participant name and all seven answers are required.");

        string root = Path.GetFullPath(Path.Combine(Application.dataPath, Application.isEditor ? "../.." : ".."));
        string path = Path.Combine(root, "questionnaire_results.csv");
        var columns = new[] { DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), participantName }.Concat(answers);
        string row = string.Join(",", columns.Select(Quote)) + "\r\n";

        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        long previousLength = file.Seek(0, SeekOrigin.End);
        string entry = previousLength == 0 ? "\uFEFF" + Header + "\r\n" + row : row;
        byte[] bytes = Encoding.UTF8.GetBytes(entry);
        try
        {
            file.Write(bytes, 0, bytes.Length);
            file.Flush();
        }
        catch (IOException)
        {
            file.SetLength(previousLength);
            throw;
        }
    }

    private static string Quote(string value)
    {
        // Keep typed names and suggestions as text when opening the CSV in a spreadsheet.
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".IndexOf(trimmed[0]) >= 0)
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
