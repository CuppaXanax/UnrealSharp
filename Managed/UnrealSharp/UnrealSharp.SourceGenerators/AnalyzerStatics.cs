﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.SourceGenerators;

public static class AnalyzerStatics
{
    public const string UStructAttribute = "UStructAttribute";
    public const string UEnumAttribute = "UEnumAttribute";
    public const string UClassAttribute = "UClassAttribute";
    public const string UInterfaceAttribute = "UInterfaceAttribute";
    public const string UMultiDelegateAttribute = "UMultiDelegateAttribute";
    public const string USingleDelegateAttribute = "USingleDelegateAttribute";

    public const string GeneratedTypeAttribute = "GeneratedTypeAttribute";
    
    public const string UPropertyAttribute = "UPropertyAttribute";
    
    public const string BindingAttribute = "BindingAttribute";
    
    public const string UObject = "UObject";
    public const string AActor = "AActor";
    
    internal static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.Name == attributeName)
            {
                return true;
            }
        }

        return false;
    }
    
    internal static bool HasAttribute(MemberDeclarationSyntax memberDecl, string attributeName)
    {
        return memberDecl.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr => attr.Name.ToString().Contains(attributeName));
    }

    internal static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeName)
    {
        INamedTypeSymbol currentSymbol = symbol;

        while (currentSymbol != null)
        {
            if (currentSymbol.Name == baseTypeName)
            {
                return true;
            }
            currentSymbol = currentSymbol.BaseType;
        }

        return false;
    }
}