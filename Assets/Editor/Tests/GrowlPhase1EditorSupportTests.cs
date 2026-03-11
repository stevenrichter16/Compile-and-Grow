using CodeEditor.Core;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class GrowlPhase1EditorSupportTests
{
    [Test]
    public void Completion_OffersPhotoProcessAndLimitingFactor()
    {
        var doc = new DocumentModel();
        doc.SetText("photo.");
        doc.SetCursor(new TextPosition(0, 6));

        var provider = new GrowlCompletionProvider();
        var result = provider.GetCompletions(doc, doc.Cursor);
        var labels = result.Items.Select(item => item.Label).ToArray();

        Assert.That(labels, Does.Contain("process"));
        Assert.That(labels, Does.Contain("get_limiting_factor"));
    }

    [Test]
    public void Completion_OffersPhase1Modules_AsIdentifiers()
    {
        var doc = new DocumentModel();
        doc.SetText("ph");
        doc.SetCursor(new TextPosition(0, 2));

        var provider = new GrowlCompletionProvider();
        var result = provider.GetCompletions(doc, doc.Cursor);
        var labels = result.Items.Select(item => item.Label).ToArray();

        Assert.That(labels, Does.Contain("photo"));
    }

    [Test]
    public void Completion_OffersLeafTrackingSupport()
    {
        var doc = new DocumentModel();
        doc.SetText("leaf.tr");
        doc.SetCursor(new TextPosition(0, 7));

        var provider = new GrowlCompletionProvider();
        var result = provider.GetCompletions(doc, doc.Cursor);
        var labels = result.Items.Select(item => item.Label).ToArray();

        Assert.That(labels, Does.Contain("track_light"));
    }

    [Test]
    public void SignatureHint_ShowsPhotoProcessSignature()
    {
        var doc = new DocumentModel();
        doc.SetText("photo.process(");
        doc.SetCursor(new TextPosition(0, 14));

        var provider = new GrowlSignatureHintProvider();
        var hint = provider.GetSignatureHint(doc, doc.Cursor, out int activeParameter);

        Assert.That(hint, Is.Not.Null);
        Assert.That(hint.Format(activeParameter), Is.EqualTo("photo.process()"));
    }

    [Test]
    public void SignatureHint_ShowsRootAbsorbSignature()
    {
        var doc = new DocumentModel();
        doc.SetText("root.absorb(");
        doc.SetCursor(new TextPosition(0, 12));

        var provider = new GrowlSignatureHintProvider();
        var hint = provider.GetSignatureHint(doc, doc.Cursor, out int activeParameter);

        Assert.That(hint, Is.Not.Null);
        Assert.That(hint.Format(activeParameter), Is.EqualTo("root.absorb(<b>resource</b>)"));
    }
}
