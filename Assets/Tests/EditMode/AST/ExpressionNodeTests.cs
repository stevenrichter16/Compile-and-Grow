using NUnit.Framework;
using System.Collections.Generic;
using GrowlLanguage.Lexer;
using GrowlLanguage.AST;

namespace GrowlLanguage.Tests.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Expression node construction tests.
    // Each test checks that a node stores its constructor arguments correctly
    // and that Line / Column come from the governing token.
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class ExpressionNodeTests
    {
        // ── Token factory helpers ────────────────────────────────────────────

        private static Token T(TokenType t, string v, int ln = 1, int col = 1)
            => new Token(t, v, ln, col);

        private static Token IntTok(string v, int ln = 1, int col = 1)
            => T(TokenType.Integer, v, ln, col);

        private static Token FloatTok(string v) => T(TokenType.Float, v);
        private static Token StrTok(string v)   => T(TokenType.String, v);
        private static Token IdTok(string v, int ln = 1, int col = 1)
            => T(TokenType.Identifier, v, ln, col);

        private static Token OpTok(TokenType t, string v) => T(t, v);

        // ── Integer literal ──────────────────────────────────────────────────

        [Test] public void IntegerLiteral_Decimal_ParsesValue()
        {
            var node = new IntegerLiteralExpr(IntTok("42", ln: 3, col: 7));
            Assert.That(node.Value,  Is.EqualTo(42L));
            Assert.That(node.Line,   Is.EqualTo(3));
            Assert.That(node.Column, Is.EqualTo(7));
        }

        [Test] public void IntegerLiteral_Hex_ParsesValue()
        {
            var node = new IntegerLiteralExpr(IntTok("0xFF"));
            Assert.That(node.Value, Is.EqualTo(255L));
        }

        [Test] public void IntegerLiteral_Binary_ParsesValue()
        {
            var node = new IntegerLiteralExpr(IntTok("0b1010"));
            Assert.That(node.Value, Is.EqualTo(10L));
        }

        // ── Float literal ─────────────────────────────────────────────────────

        [Test] public void FloatLiteral_ParsesValue()
        {
            var node = new FloatLiteralExpr(FloatTok("3.14"));
            Assert.That(node.Value, Is.EqualTo(3.14).Within(1e-10));
        }

        [Test] public void FloatLiteral_WithUnit_StoresUnit()
        {
            var tok = new Token(TokenType.Float, "5.0", 1, 1, unit: "cm");
            var node = new FloatLiteralExpr(tok);
            Assert.That(node.Unit, Is.EqualTo("cm"));
        }

        // ── String literal ────────────────────────────────────────────────────

        [Test] public void StringLiteral_StoresRawValue()
        {
            var node = new StringLiteralExpr(StrTok("hello"));
            Assert.That(node.Value, Is.EqualTo("hello"));
        }

        // ── Bool literal ──────────────────────────────────────────────────────

        [Test] public void BoolLiteral_True()
        {
            var node = new BoolLiteralExpr(T(TokenType.True, "true"));
            Assert.That(node.Value, Is.True);
        }

        [Test] public void BoolLiteral_False()
        {
            var node = new BoolLiteralExpr(T(TokenType.False, "false"));
            Assert.That(node.Value, Is.False);
        }

        // ── None literal ──────────────────────────────────────────────────────

        [Test] public void NoneLiteral_HasCorrectPosition()
        {
            var node = new NoneLiteralExpr(T(TokenType.None, "none", 2, 5));
            Assert.That(node.Line,   Is.EqualTo(2));
            Assert.That(node.Column, Is.EqualTo(5));
        }

        // ── Color literal ─────────────────────────────────────────────────────

        [Test] public void ColorLiteral_StoresHexString()
        {
            var node = new ColorLiteralExpr(T(TokenType.Color, "FF0000"));
            Assert.That(node.Hex, Is.EqualTo("FF0000"));
        }

        // ── Name expression ───────────────────────────────────────────────────

        [Test] public void NameExpr_StoresIdentifier()
        {
            var node = new NameExpr(IdTok("my_var", ln: 4, col: 2));
            Assert.That(node.Name,   Is.EqualTo("my_var"));
            Assert.That(node.Line,   Is.EqualTo(4));
            Assert.That(node.Column, Is.EqualTo(2));
        }

        // ── Binary expression ─────────────────────────────────────────────────

        [Test] public void BinaryExpr_StoresLeftOpRight()
        {
            var left  = new IntegerLiteralExpr(IntTok("2"));
            var right = new IntegerLiteralExpr(IntTok("3"));
            var op    = OpTok(TokenType.Plus, "+");
            var node  = new BinaryExpr(left, op, right);

            Assert.That(node.Left,  Is.SameAs(left));
            Assert.That(node.Right, Is.SameAs(right));
            Assert.That(node.Op.Type, Is.EqualTo(TokenType.Plus));
        }

        [Test] public void BinaryExpr_LineFromLeft()
        {
            var left  = new IntegerLiteralExpr(IntTok("2", ln: 5, col: 1));
            var right = new IntegerLiteralExpr(IntTok("3"));
            var node  = new BinaryExpr(left, OpTok(TokenType.Plus, "+"), right);
            Assert.That(node.Line, Is.EqualTo(5));
        }

        // ── Unary expression ──────────────────────────────────────────────────

        [Test] public void UnaryExpr_Negation_StoresOperandAndOp()
        {
            var operand = new IntegerLiteralExpr(IntTok("7"));
            var op      = OpTok(TokenType.Minus, "-");
            var node    = new UnaryExpr(op, operand);

            Assert.That(node.Operand, Is.SameAs(operand));
            Assert.That(node.Op.Type, Is.EqualTo(TokenType.Minus));
        }

        [Test] public void UnaryExpr_Not_StoresOp()
        {
            var operand = new BoolLiteralExpr(T(TokenType.True, "true"));
            var node    = new UnaryExpr(T(TokenType.Not, "not"), operand);
            Assert.That(node.Op.Type, Is.EqualTo(TokenType.Not));
        }

        // ── Ternary expression ────────────────────────────────────────────────

        [Test] public void TernaryExpr_StoresThreeParts()
        {
            var cond = new BoolLiteralExpr(T(TokenType.True, "true"));
            var then = new IntegerLiteralExpr(IntTok("1"));
            var els  = new IntegerLiteralExpr(IntTok("0"));
            var node = new TernaryExpr(then, cond, els);

            Assert.That(node.Condition, Is.SameAs(cond));
            Assert.That(node.ThenExpr,  Is.SameAs(then));
            Assert.That(node.ElseExpr,  Is.SameAs(els));
        }

        // ── Call expression ───────────────────────────────────────────────────

        [Test] public void CallExpr_StoresCalleeAndArgs()
        {
            var callee = new NameExpr(IdTok("grow_down"));
            var args   = new List<Argument>
            {
                new Argument(value: new IntegerLiteralExpr(IntTok("3")))
            };
            var node = new CallExpr(callee, args, line: 1, col: 1);

            Assert.That(node.Callee,     Is.SameAs(callee));
            Assert.That(node.Args.Count, Is.EqualTo(1));
        }

        [Test] public void CallExpr_NamedArg_StoresName()
        {
            var callee = new NameExpr(IdTok("f"));
            var arg    = new Argument(name: IdTok("depth"),
                                      value: new IntegerLiteralExpr(IntTok("5")));
            var node = new CallExpr(callee, new List<Argument> { arg }, 1, 1);

            Assert.That(node.Args[0].Name, Is.EqualTo("depth"));
        }

        // ── Attribute expression ──────────────────────────────────────────────

        [Test] public void AttributeExpr_StoresObjectAndField()
        {
            var obj  = new NameExpr(IdTok("org"));
            var name = IdTok("water");
            var node = new AttributeExpr(obj, name);

            Assert.That(node.Object,    Is.SameAs(obj));
            Assert.That(node.FieldName, Is.EqualTo("water"));
        }

        // ── Subscript expression ───────────────────────────────────────────────

        [Test] public void SubscriptExpr_StoresObjectAndKey()
        {
            var obj  = new NameExpr(IdTok("items"));
            var key  = new IntegerLiteralExpr(IntTok("0"));
            var node = new SubscriptExpr(obj, key);

            Assert.That(node.Object, Is.SameAs(obj));
            Assert.That(node.Key,    Is.SameAs(key));
        }

        // ── Lambda expression ─────────────────────────────────────────────────

        [Test] public void LambdaExpr_StoresParamsAndBody()
        {
            var param  = new Param(IdTok("x"));
            var body   = new BinaryExpr(new NameExpr(IdTok("x")),
                                        OpTok(TokenType.Star, "*"),
                                        new IntegerLiteralExpr(IntTok("2")));
            var node = new LambdaExpr(new List<Param> { param }, body, line: 1, col: 1);

            Assert.That(node.Params.Count, Is.EqualTo(1));
            Assert.That(node.Body,         Is.SameAs(body));
        }

        // ── List expression ───────────────────────────────────────────────────

        [Test] public void ListExpr_StoresElements()
        {
            var elems = new List<GrowlNode>
            {
                new IntegerLiteralExpr(IntTok("1")),
                new IntegerLiteralExpr(IntTok("2")),
            };
            var node = new ListExpr(elems, line: 1, col: 1);
            Assert.That(node.Elements.Count, Is.EqualTo(2));
        }

        [Test] public void ListComprehensionExpr_StoresElementAndClauses()
        {
            var element = new BinaryExpr(new NameExpr(IdTok("x")),
                                         OpTok(TokenType.Star, "*"),
                                         new NameExpr(IdTok("x")));
            var clause = new ComprehensionClause(
                targets:  new List<string> { "x" },
                iterable: new NameExpr(IdTok("items")),
                filter:   null);
            var node = new ListComprehensionExpr(element,
                                                  new List<ComprehensionClause> { clause },
                                                  line: 1, col: 1);
            Assert.That(node.Element,        Is.SameAs(element));
            Assert.That(node.Clauses.Count,  Is.EqualTo(1));
        }

        // ── Dict expression ───────────────────────────────────────────────────

        [Test] public void DictExpr_StoresEntries()
        {
            var entry = new DictEntry(key:   new StringLiteralExpr(StrTok("crop")),
                                      value: new StringLiteralExpr(StrTok("wheat")));
            var node = new DictExpr(new List<DictEntry> { entry }, line: 1, col: 1);
            Assert.That(node.Entries.Count, Is.EqualTo(1));
        }

        // ── Tuple expression ──────────────────────────────────────────────────

        [Test] public void TupleExpr_StoresElements()
        {
            var elems = new List<GrowlNode>
            {
                new IntegerLiteralExpr(IntTok("3")),
                new IntegerLiteralExpr(IntTok("4")),
            };
            var node = new TupleExpr(elems, line: 1, col: 1);
            Assert.That(node.Elements.Count, Is.EqualTo(2));
        }

        // ── Range expression ──────────────────────────────────────────────────

        [Test] public void RangeExpr_Exclusive_IsCorrect()
        {
            var start = new IntegerLiteralExpr(IntTok("0"));
            var end   = new IntegerLiteralExpr(IntTok("10"));
            var node  = new RangeExpr(start, end, inclusive: false, step: null, line: 1, col: 1);

            Assert.That(node.Start,     Is.SameAs(start));
            Assert.That(node.End,       Is.SameAs(end));
            Assert.That(node.Inclusive, Is.False);
            Assert.That(node.Step,      Is.Null);
        }

        [Test] public void RangeExpr_WithStep()
        {
            var start = new IntegerLiteralExpr(IntTok("0"));
            var end   = new IntegerLiteralExpr(IntTok("10"));
            var step  = new IntegerLiteralExpr(IntTok("2"));
            var node  = new RangeExpr(start, end, inclusive: false, step: step, line: 1, col: 1);
            Assert.That(node.Step, Is.SameAs(step));
        }

        // ── Vector expression ──────────────────────────────────────────────────

        [Test] public void VectorExpr_2D_ZIsNull()
        {
            var x = new IntegerLiteralExpr(IntTok("3"));
            var y = new IntegerLiteralExpr(IntTok("4"));
            var node = new VectorExpr(x, y, z: null, line: 1, col: 1);

            Assert.That(node.X, Is.SameAs(x));
            Assert.That(node.Y, Is.SameAs(y));
            Assert.That(node.Z, Is.Null);
        }

        [Test] public void VectorExpr_3D_ZIsSet()
        {
            var x    = new FloatLiteralExpr(FloatTok("1.0"));
            var y    = new FloatLiteralExpr(FloatTok("2.5"));
            var z    = new FloatLiteralExpr(FloatTok("-0.3"));
            var node = new VectorExpr(x, y, z, line: 1, col: 1);
            Assert.That(node.Z, Is.SameAs(z));
        }

        // ── Spread expression ──────────────────────────────────────────────────

        [Test] public void SpreadExpr_StoresOperand()
        {
            var inner = new NameExpr(IdTok("args"));
            var node  = new SpreadExpr(inner, line: 1, col: 1);
            Assert.That(node.Operand, Is.SameAs(inner));
        }

        // ── Interpolated string ────────────────────────────────────────────────

        [Test] public void InterpolatedStringExpr_StoresSegments()
        {
            var segments = new List<GrowlNode>
            {
                new StringLiteralExpr(StrTok("hello ")),
                new NameExpr(IdTok("name")),
            };
            var node = new InterpolatedStringExpr(segments, line: 1, col: 1);
            Assert.That(node.Segments.Count, Is.EqualTo(2));
        }
    }
}
