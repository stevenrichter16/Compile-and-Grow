using NUnit.Framework;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Tests.Parser
{
    [TestFixture]
    public class ParserDeclarationTests
    {
        private static ParseResult Parse(string src) => GrowlLanguage.Parser.Parser.Parse(src);

        private static ProgramNode ParseProgramNoErrors(string src)
        {
            var result = Parse(src);
            Assert.That(
                result.HasErrors,
                Is.False,
                "Parser returned errors:\n" + string.Join("\n", result.Errors.Select(e => e.ToString())));
            return result.Program;
        }

        [Test]
        public void ConstDeclaration_ParsesNameTypeAndValue()
        {
            var program = ParseProgramNoErrors("const max_energy: float = 100.0\n");
            var node = program.Statements[0] as ConstDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("max_energy"));
            Assert.That(node.TypeAnnotation, Is.Not.Null);
            Assert.That(node.TypeAnnotation.Name, Is.EqualTo("float"));
            Assert.That(node.Value, Is.TypeOf<FloatLiteralExpr>());
        }

        [Test]
        public void TypeAlias_ParsesSimpleAlias()
        {
            var program = ParseProgramNoErrors("type Energy = float\n");
            var node = program.Statements[0] as TypeAliasDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("Energy"));
            Assert.That(node.Target.Name, Is.EqualTo("float"));
        }

        [Test]
        public void TypeAlias_ParsesGenericParameters()
        {
            var program = ParseProgramNoErrors("type Box[T] = T\n");
            var node = program.Statements[0] as TypeAliasDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("Box"));
            Assert.That(node.TypeParams.Count, Is.EqualTo(1));
            Assert.That(node.TypeParams[0].Name, Is.EqualTo("T"));
            Assert.That(node.Target.Name, Is.EqualTo("T"));
        }

        [Test]
        public void StructDeclaration_ParsesFieldsAndMethods()
        {
            string src =
                "struct Genome:\n" +
                "    slots: int\n" +
                "    energy: float = 100.0\n" +
                "    fn cap(self):\n" +
                "        return self.energy\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as StructDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("Genome"));
            Assert.That(node.Fields.Count, Is.EqualTo(2));
            Assert.That(node.Fields[0].Name, Is.EqualTo("slots"));
            Assert.That(node.Fields[0].TypeAnnotation.Name, Is.EqualTo("int"));
            Assert.That(node.Fields[1].Name, Is.EqualTo("energy"));
            Assert.That(node.Fields[1].DefaultValue, Is.TypeOf<FloatLiteralExpr>());
            Assert.That(node.Methods.Count, Is.EqualTo(1));
            Assert.That(node.Methods[0].Name, Is.EqualTo("cap"));
        }

        [Test]
        public void EnumDeclaration_ParsesMembersAndMethods()
        {
            string src =
                "enum Mode:\n" +
                "    IDLE\n" +
                "    ACTIVE = 2\n" +
                "    fn label(self):\n" +
                "        return \"mode\"\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as EnumDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("Mode"));
            Assert.That(node.Members.Count, Is.EqualTo(2));
            Assert.That(node.Members[0].Name, Is.EqualTo("IDLE"));
            Assert.That(node.Members[1].Name, Is.EqualTo("ACTIVE"));
            Assert.That(node.Members[1].Value, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(node.Methods.Count, Is.EqualTo(1));
            Assert.That(node.Methods[0].Name, Is.EqualTo("label"));
        }

        [Test]
        public void TraitDeclaration_ParsesMembers()
        {
            string src =
                "trait GrowthPolicy:\n" +
                "    fn tick(self):\n" +
                "        return none\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as TraitDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("GrowthPolicy"));
            Assert.That(node.Members.Count, Is.EqualTo(1));
            Assert.That(node.Members[0], Is.TypeOf<FnDecl>());
        }

        [Test]
        public void MixinDeclaration_ParsesMethods()
        {
            string src =
                "mixin HydrationMixin:\n" +
                "    fn hydrate(self):\n" +
                "        return none\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as MixinDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("HydrationMixin"));
            Assert.That(node.Methods.Count, Is.EqualTo(1));
            Assert.That(node.Methods[0].Name, Is.EqualTo("hydrate"));
        }

        [Test]
        public void ClassDeclaration_ParsesInheritanceTraitsMixinsAndMembers()
        {
            string src =
                "class Sprout extends Plant implements Runnable, Edible with GrowMixin, LogMixin:\n" +
                "    fn tick(self):\n" +
                "        return none\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as ClassDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("Sprout"));
            Assert.That(node.Superclass, Is.Not.Null);
            Assert.That(node.Superclass.Name, Is.EqualTo("Plant"));
            Assert.That(node.Traits.Count, Is.EqualTo(2));
            Assert.That(node.Traits[0].Name, Is.EqualTo("Runnable"));
            Assert.That(node.Traits[1].Name, Is.EqualTo("Edible"));
            Assert.That(node.Mixins.Count, Is.EqualTo(2));
            Assert.That(node.Mixins[0].Name, Is.EqualTo("GrowMixin"));
            Assert.That(node.Mixins[1].Name, Is.EqualTo("LogMixin"));
            Assert.That(node.Members.Count, Is.EqualTo(1));
            Assert.That(node.Members[0], Is.TypeOf<FnDecl>());
        }

        [Test]
        public void AbstractClass_WithDecorator_ParsesFlagsAndDecorators()
        {
            string src =
                "@sealed\n" +
                "abstract class RootSystem:\n" +
                "    fn tick(self):\n" +
                "        return none\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as ClassDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.IsAbstract, Is.True);
            Assert.That(node.Decorators.Count, Is.EqualTo(1));
            Assert.That(node.Decorators[0].Name, Is.EqualTo("sealed"));
        }
    }
}
