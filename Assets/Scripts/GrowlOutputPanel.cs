using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GrowlLanguage.Runtime;

public class GrowlOutputPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TMP_Text _outputText;

    public void ShowResult(RuntimeResult result)
    {
        var sb = new StringBuilder();

        if (result.Messages.Count > 0)
        {
            sb.Append("<color=#FF6B6B>Errors:</color>\n");
            for (int i = 0; i < result.Messages.Count; i++)
            {
                sb.Append("<color=#FF6B6B>");
                sb.Append(EscapeRichText(result.Messages[i].ToString()));
                sb.Append("</color>\n");
            }
        }

        if (result.OutputLines.Count > 0)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("Output:\n");
            for (int i = 0; i < result.OutputLines.Count; i++)
            {
                sb.Append(EscapeRichText(result.OutputLines[i]));
                sb.Append('\n');
            }
        }

        if (result.Success)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("<color=#7ECE7E>Result:</color>\n");
            sb.Append("<color=#7ECE7E>");
            sb.Append(EscapeRichText(RuntimeValueFormatter.Format(result.LastValue)));
            sb.Append("</color>\n");
        }

        if (sb.Length == 0)
            sb.Append("<color=#888888>(no output)</color>");

        if (_outputText != null)
            _outputText.text = sb.ToString();

        ScrollToBottom();
    }

    public void Clear()
    {
        if (_outputText != null)
            _outputText.text = string.Empty;
    }

    public void AppendLine(string line)
    {
        if (_outputText != null)
            _outputText.text += EscapeRichText(line) + "\n";

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private static string EscapeRichText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("<", "<\u200B");
    }
}
