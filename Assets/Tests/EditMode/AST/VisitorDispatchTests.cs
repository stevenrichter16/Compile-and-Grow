using NUnit.Framework;
using System.Collections.Generic;
using GrowlLanguage.Lexer;
using GrowlLanguage.AST;

namespace GrowlLanguage.Tests.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Visitor dispatch tests.
    //
    // RecordingVisitor records the name of the last VisitXxx method called.
    // Every concrete node type is constructed minimally and Accept()ed to
    // verify that Accept() routes to exactly the right method.
    //
    // This test is the contract between node authors and visitor authors:
    // every node must call exactly one Visit method, and that method must
    // match the node's type.
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class VisitorDispatchTests
    {
        // ── Recording visitor ─────────────────────────────────────────────────

        private sealed class RecordingVisitor : GrowlVisitorBase<string>
        {
            public string LastVisited { get; private set; }

            private string Record(string name) { LastVisited = name; return name; }

            public override string VisitIntegerLiteral  (IntegerLiteralExpr   n) => Record("IntegerLiteral");
            public override string VisitFloatLiteral    (FloatLiteralExpr     n) => Record("FloatLiteral");
            public override string VisitStringLiteral   (StringLiteralExpr    n) => Record("StringLiteral");
            public override string VisitBoolLiteral     (BoolLiteralExpr      n) => Record("BoolLiteral");
            public override string VisitNoneLiteral     (NoneLiteralExpr      n) => Record("NoneLiteral");
            public override string VisitColorLiteral    (ColorLiteralExpr     n) => Record("ColorLiteral");
            public override string VisitInterpolatedStr (InterpolatedStringExpr n) => Record("InterpolatedStr");
            public override string VisitName            (NameExpr             n) => Record("Name");
            public override string VisitBinary          (BinaryExpr           n) => Record("Binary");
            public override string VisitUnary           (UnaryExpr            n) => Record("Unary");
            public override string VisitTernary         (TernaryExpr          n) => Record("Ternary");
            public override string VisitCall            (CallExpr             n) => Record("Call");
            public override string VisitAttribute       (AttributeExpr        n) => Record("Attribute");
            public override string VisitSubscript       (SubscriptExpr        n) => Record("Subscript");
            public override string VisitLambda          (LambdaExpr           n) => Record("Lambda");
            public override string VisitList            (ListExpr             n) => Record("List");
            public override string VisitListComprehension(ListComprehensionExpr n) => Record("ListComprehension");
            public override string VisitDict            (DictExpr             n) => Record("Dict");
            public override string VisitDictComprehension(DictComprehensionExpr n) => Record("DictComprehension");
            public override string VisitSet             (SetExpr              n) => Record("Set");
            public override string VisitTuple           (TupleExpr            n) => Record("Tuple");
            public override string VisitRange           (RangeExpr            n) => Record("Range");
            public override string VisitVector          (VectorExpr           n) => Record("Vector");
            public override string VisitSpread          (SpreadExpr           n) => Record("Spread");
            public override string VisitAssign          (AssignStmt           n) => Record("Assign");
            public override string VisitExprStmt        (ExprStmt             n) => Record("ExprStmt");
            public override string VisitReturn          (ReturnStmt           n) => Record("Return");
            public override string VisitBreak           (BreakStmt            n) => Record("Break");
            public override string VisitContinue        (ContinueStmt         n) => Record("Continue");
            public override string VisitYield           (YieldStmt            n) => Record("Yield");
            public override string VisitWait            (WaitStmt             n) => Record("Wait");
            public override string VisitDefer           (DeferStmt            n) => Record("Defer");
            public override string VisitMutate          (MutateStmt           n) => Record("Mutate");
            public override string VisitIf              (IfStmt               n) => Record("If");
            public override string VisitFor             (ForStmt              n) => Record("For");
            public override string VisitWhile           (WhileStmt            n) => Record("While");
            public override string VisitLoop            (LoopStmt             n) => Record("Loop");
            public override string VisitMatch           (MatchStmt            n) => Record("Match");
            public override string VisitTry             (TryStmt              n) => Record("Try");
            public override string VisitImport          (ImportStmt           n) => Record("Import");
            public override string VisitModule          (ModuleDecl           n) => Record("Module");
            public override string VisitFn              (FnDecl               n) => Record("Fn");
            public override string VisitClass           (ClassDecl            n) => Record("Class");
            public override string VisitStruct          (StructDecl           n) => Record("Struct");
            public override string VisitEnum            (EnumDecl             n) => Record("Enum");
            public override string VisitTrait           (TraitDecl            n) => Record("Trait");
            public override string VisitMixin           (MixinDecl            n) => Record("Mixin");
            public override string VisitConst           (ConstDecl            n) => Record("Const");
            public override string VisitTypeAlias       (TypeAliasDecl        n) => Record("TypeAlias");
            public override string VisitGene            (GeneDecl             n) => Record("Gene");
            public override string VisitPhase           (PhaseBlock           n) => Record("Phase");
            public override string VisitWhen            (WhenBlock            n) => Record("When");
            public override string VisitRespond         (RespondBlock         n) => Record("Respond");
            public override string VisitAdapt           (AdaptBlock           n) => Record("Adapt");
            public override string VisitCycle           (CycleBlock           n) => Record("Cycle");
            public override string VisitTicker          (TickerDecl           n) => Record("Ticker");
            public override string VisitProgram         (ProgramNode          n) => Record("Program");
        }

        // ── Minimal node factory helpers ──────────────────────────────────────

        private static Token T(TokenType t, string v = "") => new Token(t, v, 1, 1);
        private static Token Id(string v) => T(TokenType.Identifier, v);
        private static Token Int(string v) => T(TokenType.Integer, v);
        private static IntegerLiteralExpr IntLit(string v = "0")
            => new IntegerLiteralExpr(Int(v));
        private static NameExpr NameN(string v = "x")
            => new NameExpr(Id(v));
        private static List<GrowlNode> EmptyBlock() => new List<GrowlNode>();
        private static List<Argument>  NoArgs()     => new List<Argument>();
        private static List<Param>     NoParams()   => new List<Param>();
        private static List<Decorator> NoDecs()     => new List<Decorator>();

        // ── One test per concrete node type ───────────────────────────────────

        private RecordingVisitor _v;

        [SetUp] public void Setup() => _v = new RecordingVisitor();

        private void Dispatch(GrowlNode node) { node.Accept(_v); }

        [Test] public void Dispatches_IntegerLiteral()
        { Dispatch(IntLit("1"));                                     Assert.That(_v.LastVisited, Is.EqualTo("IntegerLiteral")); }

        [Test] public void Dispatches_FloatLiteral()
        { Dispatch(new FloatLiteralExpr(T(TokenType.Float, "1.0"))); Assert.That(_v.LastVisited, Is.EqualTo("FloatLiteral")); }

        [Test] public void Dispatches_StringLiteral()
        { Dispatch(new StringLiteralExpr(T(TokenType.String, "hi"))); Assert.That(_v.LastVisited, Is.EqualTo("StringLiteral")); }

        [Test] public void Dispatches_BoolLiteral()
        { Dispatch(new BoolLiteralExpr(T(TokenType.True, "true")));  Assert.That(_v.LastVisited, Is.EqualTo("BoolLiteral")); }

        [Test] public void Dispatches_NoneLiteral()
        { Dispatch(new NoneLiteralExpr(T(TokenType.None, "none")));  Assert.That(_v.LastVisited, Is.EqualTo("NoneLiteral")); }

        [Test] public void Dispatches_ColorLiteral()
        { Dispatch(new ColorLiteralExpr(T(TokenType.Color, "FF0000"))); Assert.That(_v.LastVisited, Is.EqualTo("ColorLiteral")); }

        [Test] public void Dispatches_InterpolatedStr()
        { Dispatch(new InterpolatedStringExpr(new List<GrowlNode>(), 1, 1)); Assert.That(_v.LastVisited, Is.EqualTo("InterpolatedStr")); }

        [Test] public void Dispatches_Name()
        { Dispatch(NameN());                                          Assert.That(_v.LastVisited, Is.EqualTo("Name")); }

        [Test] public void Dispatches_Binary()
        { Dispatch(new BinaryExpr(IntLit(), T(TokenType.Plus, "+"), IntLit())); Assert.That(_v.LastVisited, Is.EqualTo("Binary")); }

        [Test] public void Dispatches_Unary()
        { Dispatch(new UnaryExpr(T(TokenType.Minus, "-"), IntLit()));  Assert.That(_v.LastVisited, Is.EqualTo("Unary")); }

        [Test] public void Dispatches_Ternary()
        { Dispatch(new TernaryExpr(IntLit(), NameN(), IntLit()));      Assert.That(_v.LastVisited, Is.EqualTo("Ternary")); }

        [Test] public void Dispatches_Call()
        { Dispatch(new CallExpr(NameN(), NoArgs(), 1, 1));             Assert.That(_v.LastVisited, Is.EqualTo("Call")); }

        [Test] public void Dispatches_Attribute()
        { Dispatch(new AttributeExpr(NameN(), Id("field")));           Assert.That(_v.LastVisited, Is.EqualTo("Attribute")); }

        [Test] public void Dispatches_Subscript()
        { Dispatch(new SubscriptExpr(NameN(), IntLit()));               Assert.That(_v.LastVisited, Is.EqualTo("Subscript")); }

        [Test] public void Dispatches_Lambda()
        { Dispatch(new LambdaExpr(NoParams(), IntLit(), 1, 1));        Assert.That(_v.LastVisited, Is.EqualTo("Lambda")); }

        [Test] public void Dispatches_List()
        { Dispatch(new ListExpr(new List<GrowlNode>(), 1, 1));         Assert.That(_v.LastVisited, Is.EqualTo("List")); }

        [Test] public void Dispatches_ListComprehension()
        { Dispatch(new ListComprehensionExpr(IntLit(), new List<ComprehensionClause>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("ListComprehension")); }

        [Test] public void Dispatches_Dict()
        { Dispatch(new DictExpr(new List<DictEntry>(), 1, 1));         Assert.That(_v.LastVisited, Is.EqualTo("Dict")); }

        [Test] public void Dispatches_DictComprehension()
        { Dispatch(new DictComprehensionExpr(NameN(), IntLit(), new List<ComprehensionClause>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("DictComprehension")); }

        [Test] public void Dispatches_Set()
        { Dispatch(new SetExpr(new List<GrowlNode>(), 1, 1));          Assert.That(_v.LastVisited, Is.EqualTo("Set")); }

        [Test] public void Dispatches_Tuple()
        { Dispatch(new TupleExpr(new List<GrowlNode>(), 1, 1));        Assert.That(_v.LastVisited, Is.EqualTo("Tuple")); }

        [Test] public void Dispatches_Range()
        { Dispatch(new RangeExpr(IntLit(), IntLit(), false, null, 1, 1)); Assert.That(_v.LastVisited, Is.EqualTo("Range")); }

        [Test] public void Dispatches_Vector()
        { Dispatch(new VectorExpr(IntLit(), IntLit(), null, 1, 1));    Assert.That(_v.LastVisited, Is.EqualTo("Vector")); }

        [Test] public void Dispatches_Spread()
        { Dispatch(new SpreadExpr(NameN(), 1, 1));                     Assert.That(_v.LastVisited, Is.EqualTo("Spread")); }

        [Test] public void Dispatches_Assign()
        { Dispatch(new AssignStmt(NameN(), T(TokenType.Equal, "="), IntLit())); Assert.That(_v.LastVisited, Is.EqualTo("Assign")); }

        [Test] public void Dispatches_ExprStmt()
        { Dispatch(new ExprStmt(NameN()));                             Assert.That(_v.LastVisited, Is.EqualTo("ExprStmt")); }

        [Test] public void Dispatches_Return()
        { Dispatch(new ReturnStmt(T(TokenType.Return, "return"), null)); Assert.That(_v.LastVisited, Is.EqualTo("Return")); }

        [Test] public void Dispatches_Break()
        { Dispatch(new BreakStmt(T(TokenType.Break, "break")));        Assert.That(_v.LastVisited, Is.EqualTo("Break")); }

        [Test] public void Dispatches_Continue()
        { Dispatch(new ContinueStmt(T(TokenType.Continue, "continue"))); Assert.That(_v.LastVisited, Is.EqualTo("Continue")); }

        [Test] public void Dispatches_Yield()
        { Dispatch(new YieldStmt(T(TokenType.Yield, "yield"), IntLit())); Assert.That(_v.LastVisited, Is.EqualTo("Yield")); }

        [Test] public void Dispatches_Wait()
        { Dispatch(new WaitStmt(T(TokenType.Wait, "wait"), IntLit())); Assert.That(_v.LastVisited, Is.EqualTo("Wait")); }

        [Test] public void Dispatches_Defer()
        { Dispatch(new DeferStmt(IntLit(), isUntil: false, body: EmptyBlock(), line: 1, col: 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Defer")); }

        [Test] public void Dispatches_Mutate()
        { Dispatch(new MutateStmt(NameN(), IntLit(), interval: null, line: 1, col: 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Mutate")); }

        [Test] public void Dispatches_If()
        { Dispatch(new IfStmt(NameN(), EmptyBlock(), new List<ElifClause>(), null, 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("If")); }

        [Test] public void Dispatches_For()
        { Dispatch(new ForStmt(new List<string> { "x" }, NameN(), EmptyBlock(), null, 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("For")); }

        [Test] public void Dispatches_While()
        { Dispatch(new WhileStmt(NameN(), EmptyBlock(), 1, 1));        Assert.That(_v.LastVisited, Is.EqualTo("While")); }

        [Test] public void Dispatches_Loop()
        { Dispatch(new LoopStmt(EmptyBlock(), 1, 1));                  Assert.That(_v.LastVisited, Is.EqualTo("Loop")); }

        [Test] public void Dispatches_Match()
        { Dispatch(new MatchStmt(NameN(), new List<CaseClause>(), 1, 1)); Assert.That(_v.LastVisited, Is.EqualTo("Match")); }

        [Test] public void Dispatches_Try()
        { Dispatch(new TryStmt(EmptyBlock(), Id("e"), EmptyBlock(), null, 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Try")); }

        [Test] public void Dispatches_Import()
        { Dispatch(new ImportStmt(new List<string> { "math" }, null, null, 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Import")); }

        [Test] public void Dispatches_Module()
        { Dispatch(new ModuleDecl(Id("drought"), 1, 1));               Assert.That(_v.LastVisited, Is.EqualTo("Module")); }

        [Test] public void Dispatches_Fn()
        { Dispatch(new FnDecl(Id("f"), NoParams(), null, EmptyBlock(), NoDecs(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Fn")); }

        [Test] public void Dispatches_Class()
        { Dispatch(new ClassDecl(Id("C"), null, new List<TypeRef>(), new List<TypeRef>(),
                                 new List<GrowlNode>(), false, NoDecs(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Class")); }

        [Test] public void Dispatches_Struct()
        { Dispatch(new StructDecl(Id("S"), new List<FieldDecl>(), new List<FnDecl>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Struct")); }

        [Test] public void Dispatches_Enum()
        { Dispatch(new EnumDecl(Id("E"), new List<TypeRef>(), new List<EnumMember>(),
                                new List<FnDecl>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Enum")); }

        [Test] public void Dispatches_Trait()
        { Dispatch(new TraitDecl(Id("T"), new List<GrowlNode>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Trait")); }

        [Test] public void Dispatches_Mixin()
        { Dispatch(new MixinDecl(Id("M"), new List<FnDecl>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Mixin")); }

        [Test] public void Dispatches_Const()
        { Dispatch(new ConstDecl(Id("C"), null, IntLit(), 1, 1));      Assert.That(_v.LastVisited, Is.EqualTo("Const")); }

        [Test] public void Dispatches_TypeAlias()
        { Dispatch(new TypeAliasDecl(Id("Energy"), new TypeRef("float"), null, 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("TypeAlias")); }

        [Test] public void Dispatches_Gene()
        {
            var fn = new FnDecl(Id("intake"), NoParams(), null, EmptyBlock(), NoDecs(), 1, 1);
            Dispatch(new GeneDecl("intake", isRole: true, fn, 1, 1));
            Assert.That(_v.LastVisited, Is.EqualTo("Gene"));
        }

        [Test] public void Dispatches_Phase()
        { Dispatch(new PhaseBlock("seedling", IntLit("0"), IntLit("2"),
                                  condition: null, EmptyBlock(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Phase")); }

        [Test] public void Dispatches_When()
        { Dispatch(new WhenBlock(NameN(), EmptyBlock(), thenBlock: null, 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("When")); }

        [Test] public void Dispatches_Respond()
        { Dispatch(new RespondBlock("damage", binding: null, EmptyBlock(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Respond")); }

        [Test] public void Dispatches_Adapt()
        { Dispatch(new AdaptBlock(NameN(), new List<AdaptRule>(), IntLit(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Adapt")); }

        [Test] public void Dispatches_Cycle()
        { Dispatch(new CycleBlock("heartbeat", IntLit("10"), new List<CyclePoint>(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Cycle")); }

        [Test] public void Dispatches_Ticker()
        { Dispatch(new TickerDecl("heartbeat", IntLit("10"), EmptyBlock(), 1, 1));
          Assert.That(_v.LastVisited, Is.EqualTo("Ticker")); }

        [Test] public void Dispatches_Program()
        { Dispatch(new ProgramNode(EmptyBlock()));                     Assert.That(_v.LastVisited, Is.EqualTo("Program")); }
    }
}
