using System;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Convenience base class — every Visit method throws NotImplementedException
    // so that subclasses only override what they actually need.
    // ─────────────────────────────────────────────────────────────────────────
    public abstract class GrowlVisitorBase<T> : IGrowlVisitor<T>
    {
        // ── Literals ──────────────────────────────────────────────────────────
        public virtual T VisitIntegerLiteral  (IntegerLiteralExpr    node) => throw new NotImplementedException(nameof(VisitIntegerLiteral));
        public virtual T VisitFloatLiteral    (FloatLiteralExpr      node) => throw new NotImplementedException(nameof(VisitFloatLiteral));
        public virtual T VisitStringLiteral   (StringLiteralExpr     node) => throw new NotImplementedException(nameof(VisitStringLiteral));
        public virtual T VisitBoolLiteral     (BoolLiteralExpr       node) => throw new NotImplementedException(nameof(VisitBoolLiteral));
        public virtual T VisitNoneLiteral     (NoneLiteralExpr       node) => throw new NotImplementedException(nameof(VisitNoneLiteral));
        public virtual T VisitColorLiteral    (ColorLiteralExpr      node) => throw new NotImplementedException(nameof(VisitColorLiteral));
        public virtual T VisitInterpolatedStr (InterpolatedStringExpr node) => throw new NotImplementedException(nameof(VisitInterpolatedStr));

        // ── Core Expressions ─────────────────────────────────────────────────
        public virtual T VisitName            (NameExpr              node) => throw new NotImplementedException(nameof(VisitName));
        public virtual T VisitBinary          (BinaryExpr            node) => throw new NotImplementedException(nameof(VisitBinary));
        public virtual T VisitUnary           (UnaryExpr             node) => throw new NotImplementedException(nameof(VisitUnary));
        public virtual T VisitTernary         (TernaryExpr           node) => throw new NotImplementedException(nameof(VisitTernary));
        public virtual T VisitCall            (CallExpr              node) => throw new NotImplementedException(nameof(VisitCall));
        public virtual T VisitAttribute       (AttributeExpr         node) => throw new NotImplementedException(nameof(VisitAttribute));
        public virtual T VisitSubscript       (SubscriptExpr         node) => throw new NotImplementedException(nameof(VisitSubscript));
        public virtual T VisitLambda          (LambdaExpr            node) => throw new NotImplementedException(nameof(VisitLambda));

        // ── Collection Expressions ────────────────────────────────────────────
        public virtual T VisitList            (ListExpr              node) => throw new NotImplementedException(nameof(VisitList));
        public virtual T VisitListComprehension(ListComprehensionExpr node) => throw new NotImplementedException(nameof(VisitListComprehension));
        public virtual T VisitDict            (DictExpr              node) => throw new NotImplementedException(nameof(VisitDict));
        public virtual T VisitDictComprehension(DictComprehensionExpr node) => throw new NotImplementedException(nameof(VisitDictComprehension));
        public virtual T VisitSet             (SetExpr               node) => throw new NotImplementedException(nameof(VisitSet));
        public virtual T VisitTuple           (TupleExpr             node) => throw new NotImplementedException(nameof(VisitTuple));
        public virtual T VisitRange           (RangeExpr             node) => throw new NotImplementedException(nameof(VisitRange));
        public virtual T VisitVector          (VectorExpr            node) => throw new NotImplementedException(nameof(VisitVector));
        public virtual T VisitSpread          (SpreadExpr            node) => throw new NotImplementedException(nameof(VisitSpread));

        // ── Statements ────────────────────────────────────────────────────────
        public virtual T VisitAssign          (AssignStmt            node) => throw new NotImplementedException(nameof(VisitAssign));
        public virtual T VisitExprStmt        (ExprStmt              node) => throw new NotImplementedException(nameof(VisitExprStmt));
        public virtual T VisitReturn          (ReturnStmt            node) => throw new NotImplementedException(nameof(VisitReturn));
        public virtual T VisitBreak           (BreakStmt             node) => throw new NotImplementedException(nameof(VisitBreak));
        public virtual T VisitContinue        (ContinueStmt          node) => throw new NotImplementedException(nameof(VisitContinue));
        public virtual T VisitYield           (YieldStmt             node) => throw new NotImplementedException(nameof(VisitYield));
        public virtual T VisitWait            (WaitStmt              node) => throw new NotImplementedException(nameof(VisitWait));
        public virtual T VisitDefer           (DeferStmt             node) => throw new NotImplementedException(nameof(VisitDefer));
        public virtual T VisitMutate          (MutateStmt            node) => throw new NotImplementedException(nameof(VisitMutate));

        // ── Control Flow ─────────────────────────────────────────────────────
        public virtual T VisitIf              (IfStmt                node) => throw new NotImplementedException(nameof(VisitIf));
        public virtual T VisitFor             (ForStmt               node) => throw new NotImplementedException(nameof(VisitFor));
        public virtual T VisitWhile           (WhileStmt             node) => throw new NotImplementedException(nameof(VisitWhile));
        public virtual T VisitLoop            (LoopStmt              node) => throw new NotImplementedException(nameof(VisitLoop));
        public virtual T VisitMatch           (MatchStmt             node) => throw new NotImplementedException(nameof(VisitMatch));
        public virtual T VisitTry             (TryStmt               node) => throw new NotImplementedException(nameof(VisitTry));

        // ── Import / Module ───────────────────────────────────────────────────
        public virtual T VisitImport          (ImportStmt            node) => throw new NotImplementedException(nameof(VisitImport));
        public virtual T VisitModule          (ModuleDecl            node) => throw new NotImplementedException(nameof(VisitModule));

        // ── Declarations ─────────────────────────────────────────────────────
        public virtual T VisitFn              (FnDecl                node) => throw new NotImplementedException(nameof(VisitFn));
        public virtual T VisitClass           (ClassDecl             node) => throw new NotImplementedException(nameof(VisitClass));
        public virtual T VisitStruct          (StructDecl            node) => throw new NotImplementedException(nameof(VisitStruct));
        public virtual T VisitEnum            (EnumDecl              node) => throw new NotImplementedException(nameof(VisitEnum));
        public virtual T VisitTrait           (TraitDecl             node) => throw new NotImplementedException(nameof(VisitTrait));
        public virtual T VisitMixin           (MixinDecl             node) => throw new NotImplementedException(nameof(VisitMixin));
        public virtual T VisitConst           (ConstDecl             node) => throw new NotImplementedException(nameof(VisitConst));
        public virtual T VisitTypeAlias       (TypeAliasDecl         node) => throw new NotImplementedException(nameof(VisitTypeAlias));

        // ── Biological Constructs ─────────────────────────────────────────────
        public virtual T VisitGene            (GeneDecl              node) => throw new NotImplementedException(nameof(VisitGene));
        public virtual T VisitPhase           (PhaseBlock            node) => throw new NotImplementedException(nameof(VisitPhase));
        public virtual T VisitWhen            (WhenBlock             node) => throw new NotImplementedException(nameof(VisitWhen));
        public virtual T VisitRespond         (RespondBlock          node) => throw new NotImplementedException(nameof(VisitRespond));
        public virtual T VisitAdapt           (AdaptBlock            node) => throw new NotImplementedException(nameof(VisitAdapt));
        public virtual T VisitCycle           (CycleBlock            node) => throw new NotImplementedException(nameof(VisitCycle));
        public virtual T VisitTicker          (TickerDecl            node) => throw new NotImplementedException(nameof(VisitTicker));

        // ── Program ──────────────────────────────────────────────────────────
        public virtual T VisitProgram         (ProgramNode           node) => throw new NotImplementedException(nameof(VisitProgram));
    }
}
