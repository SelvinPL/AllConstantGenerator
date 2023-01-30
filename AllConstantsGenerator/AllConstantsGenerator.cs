using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

[Generator]
public class AllConstantsGenerator : IIncrementalGenerator
{
	internal const string AddAllStringConstantsAttributeNamespace = "AllConstantsGenerator";
	internal const string AddAllStringConstantsAttributeClass = "AddAllStringConstantsAttribute";
	internal const string AddAllStringConstantsAttributeFull = AddAllStringConstantsAttributeNamespace + "." + AddAllStringConstantsAttributeClass;


	static (MemberDeclarationSyntax Root, string FullName) BuildSyntaxTreeUpToFirstNamespaceIfExists(TypeDeclarationSyntax syntax, SyntaxList<MemberDeclarationSyntax> members)
	{
		MemberDeclarationSyntax last = null;
		var fullName = new StringBuilder(syntax.Identifier.Text);
		var typeParameters = syntax.TypeParameterList?.ToString();
		if(typeParameters != null)
		{
			fullName.Append(typeParameters.Replace('<', '(')?.Replace('>', ')'));
		}
		while(true)
		{

			if(last != null)
			{
				typeParameters = syntax.TypeParameterList?.ToString();
				if(typeParameters != null)
				{
					typeParameters = typeParameters.Replace('<', '(')?.Replace('>', ')');
					fullName.Insert(0, '.').Insert(0, typeParameters).Insert(0, syntax.Identifier.Text);
				}
				else
				{
					fullName.Insert(0, '.').Insert(0, syntax.Identifier.Text);
				}
				members = last.AsList();
			}
			last = syntax
				.WithAttributeLists(new SyntaxList<AttributeListSyntax>())
				.WithModifiers(new SyntaxTokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
				.WithMembers(members);
			if(!(syntax.Parent is TypeDeclarationSyntax temp))
			{
				break;
			}
			syntax = temp;
		}
		SyntaxNode iterate = syntax;
		while(true)
		{
			if(iterate.Parent is NamespaceDeclarationSyntax ns)
			{
				fullName.Insert(0, '.').Insert(0, ns.Name);
				last = SyntaxFactory.NamespaceDeclaration(ns.Name, ns.Externs, ns.Usings, last.AsList());
				iterate = iterate.Parent;
				continue;
			}
			if(iterate.Parent is FileScopedNamespaceDeclarationSyntax fs)
			{
				fullName.Insert(0, '.').Insert(0, fs.Name);
				last = SyntaxFactory.FileScopedNamespaceDeclaration(new SyntaxList<AttributeListSyntax>(),
					fs.Modifiers, fs.NamespaceKeyword, fs.Name, fs.SemicolonToken, fs.Externs, fs.Usings, last.AsList());
				iterate = iterate.Parent;
				continue;
			}
			break;
		}
		return (last, fullName.ToString());
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		//generate code for SourceGeneratoConstantTest.AddAllStringConstantsAttribute class
		context.RegisterPostInitializationOutput(i =>
		{
			var attribute = SyntaxFactory.CompilationUnit().WithMembers(
				SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(AddAllStringConstantsAttributeNamespace))
				.WithMembers(
					SyntaxFactory.ClassDeclaration(AddAllStringConstantsAttributeClass).WithBaseList(
						SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
							SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("System.Attribute"))))
						).AsList()).AsList());
			i.AddSource(AddAllStringConstantsAttributeClass + "-AllConstantsGenerator.g.cs", attribute.NormalizeWhitespace().ToFullString());
		});
		//search all classes with SourceGeneratoConstantTest.AddAllStringConstants
		var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
			(node, _) => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0,
			(ctx, _) => ((ClassDeclarationSyntax)ctx.Node).AttributeLists
				.Any(attributeListSyntax => attributeListSyntax.Attributes
					.Any(attributeSyntax =>
						ModelExtensions.GetSymbolInfo(ctx.SemanticModel, attributeSyntax).Symbol is IMethodSymbol attributeSymbol &&
						attributeSymbol.ContainingType.ToDisplayString() == AddAllStringConstantsAttributeFull)) ? (ClassDeclarationSyntax)ctx.Node : null)
			.Where(m => m != null);

		var compilationAndValues = context.CompilationProvider.Combine(classDeclarations.Collect());
		context.RegisterSourceOutput(compilationAndValues,
			(spc, source) => Execute(source.Left, source.Right, spc));
	}

	private DiagnosticDescriptor error = new DiagnosticDescriptor("CG0001", "Error", "{0}", "Other", DiagnosticSeverity.Error, true);

	private void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
	{
		try
		{

			foreach(ClassDeclarationSyntax classDeclarationSyntax in classes)
			{
				var stringArrayType = (ArrayTypeSyntax)SyntaxFactory.ParseTypeName("string[]");
				var constVariables = classDeclarationSyntax.Members.OfType<FieldDeclarationSyntax>()
					.Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword) && f.IsString(compilation)))
					.SelectMany(c => c.Declaration.Variables).Select(x => x.Identifier).ToList();
				var initializerNode = SyntaxFactory.ArrayCreationExpression(stringArrayType)
					.WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression)
					.WithExpressions(SyntaxFactory.SeparatedList<ExpressionSyntax>(
						constVariables.Select(x => SyntaxFactory.IdentifierName(x.ValueText)),
					Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), constVariables.Count - 1))));
				var variableDeclaration = SyntaxFactory.VariableDeclaration(stringArrayType)
					.AddVariables(SyntaxFactory.VariableDeclarator("ALL")
					.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken),
						initializerNode)));
				var field = SyntaxFactory.FieldDeclaration(variableDeclaration)
					.WithModifiers(new SyntaxTokenList(
						SyntaxFactory.Token(SyntaxKind.PublicKeyword),
						SyntaxFactory.Token(SyntaxKind.StaticKeyword),
						SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
				var tree = BuildSyntaxTreeUpToFirstNamespaceIfExists(classDeclarationSyntax, field.AsList());
				var code = tree.Root.NormalizeWhitespace().ToFullString();
				context.AddSource(tree.FullName + "-AllConstantsGenerator.g.cs", code);
			}
		}
		catch(Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(error, Location.None, ex.Message));
		}
	}
}

static class Ext
{
	public static bool IsString(this FieldDeclarationSyntax field, Compilation compilation)
	{
		return (compilation.GetSemanticModel(field.SyntaxTree).GetSymbolInfo(field.Declaration.Type).Symbol as ITypeSymbol).SpecialType == SpecialType.System_String;
	}

	public static SyntaxList<MemberDeclarationSyntax> AsList(this MemberDeclarationSyntax single)
		=> new SyntaxList<MemberDeclarationSyntax>(single);
}