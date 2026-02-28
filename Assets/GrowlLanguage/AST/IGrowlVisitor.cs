namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Visitor interface — one method per concrete node type.
    //
    // Implementations may choose to handle only a subset; use GrowlVisitorBase
    // as a base class to get default NotImplementedException stubs.
    // ─────────────────────────────────────────────────────────────────────────
    public interface IGrowlVisitor<out T>
    {
        // ── Literals ─────────────────────────────────────────────────────────
        T VisitIntegerLiteral   (IntegerLiteralExpr    node);
        T VisitFloatLiteral     (FloatLiteralExpr      node);
        T VisitStringLiteral    (StringLiteralExpr     node);
        T VisitBoolLiteral      (BoolLiteralExpr       node);
        T VisitNoneLiteral      (NoneLiteralExpr       node);
        T VisitColorLiteral     (ColorLiteralExpr      node);
        T VisitInterpolatedStr  (InterpolatedStringExpr node);

        // ── Core Expressions ─────────────────────────────────────────────────
        T VisitName             (NameExpr              node);
        T VisitBinary           (BinaryExpr            node);
        T VisitUnary            (UnaryExpr             node);
        T VisitTernary          (TernaryExpr           node);
        T VisitCall             (CallExpr              node);
        T VisitAttribute        (AttributeExpr         node);
        T VisitSubscript        (SubscriptExpr         node);
        T VisitLambda           (LambdaExpr            node);

        // ── Collection Expressions ────────────────────────────────────────────
        T VisitList             (ListExpr              node);
        T VisitListComprehension(ListComprehensionExpr node);
        T VisitDict             (DictExpr              node);
        T VisitDictComprehension(DictComprehensionExpr node);
        T VisitSet              (SetExpr               node);
        T VisitTuple            (TupleExpr             node);
        T VisitRange            (RangeExpr             node);
        T VisitVector           (VectorExpr            node);
        T VisitSpread           (SpreadExpr            node);

        // ── Statements ────────────────────────────────────────────────────────
        T VisitAssign           (AssignStmt            node);
        T VisitExprStmt         (ExprStmt              node);
        T VisitReturn           (ReturnStmt            node);
        T VisitBreak            (BreakStmt             node);
        T VisitContinue         (ContinueStmt          node);
        T VisitYield            (YieldStmt             node);
        T VisitWait             (WaitStmt              node);
        T VisitDefer            (DeferStmt             node);
        T VisitMutate           (MutateStmt            node);

        // ── Control Flow ─────────────────────────────────────────────────────
        T VisitIf               (IfStmt                node);
        T VisitFor              (ForStmt               node);
        T VisitWhile            (WhileStmt             node);
        T VisitLoop             (LoopStmt              node);
        T VisitMatch            (MatchStmt             node);
        T VisitTry              (TryStmt               node);

        // ── Import / Module ───────────────────────────────────────────────────
        T VisitImport           (ImportStmt            node);
        T VisitModule           (ModuleDecl            node);

        // ── Declarations ─────────────────────────────────────────────────────
        T VisitFn               (FnDecl                node);
        T VisitClass            (ClassDecl             node);
        T VisitStruct           (StructDecl            node);
        T VisitEnum             (EnumDecl              node);
        T VisitTrait            (TraitDecl             node);
        T VisitMixin            (MixinDecl             node);
        T VisitConst            (ConstDecl             node);
        T VisitTypeAlias        (TypeAliasDecl         node);

        // ── Biological Constructs ─────────────────────────────────────────────
        T VisitGene             (GeneDecl              node);
        T VisitPhase            (PhaseBlock            node);
        T VisitWhen             (WhenBlock             node);
        T VisitRespond          (RespondBlock          node);
        T VisitAdapt            (AdaptBlock            node);
        T VisitCycle            (CycleBlock            node);
        T VisitTicker           (TickerDecl            node);

        // ── Program ──────────────────────────────────────────────────────────
        T VisitProgram          (ProgramNode           node);
    }
}
