﻿using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public class GeneratorStringBuilder : IDisposable
{
    private int _indent;
    private List<string> _directives = new();
    private readonly BorrowStringBuilder _borrower = new(StringBuilderCache.Big);
    public StringBuilder StringBuilder => _borrower.StringBuilder;

    public override string ToString()
    {
        return StringBuilder.ToString();
    }

    public void Dispose()
    {
        _borrower.Dispose();
    }
    
    public void OpenBrace()
    {
        AppendLine("{");
        Indent();
    }
    
    public void CloseBrace()
    {
        UnIndent();
        AppendLine("}");
    }

    public void Indent()
    {
       ++_indent;
    }
    
    public void UnIndent()
    {
        --_indent;
    }

    public void AppendLine()
    {
        if (StringBuilder.Length > 0)
        {
            StringBuilder.AppendLine();
        }
        
        for (int i = 0; i < _indent; i++)
        {
            StringBuilder.Append("    ");
        }
    }
    
    public void AppendLine(string line)
    {
        AppendLine();
        StringBuilder.Append(line);
    }
    
    public void CloseBraceWithSemicolon()
    {
        AppendLine("};");
    }
    
    public void DeclareDirective(string directive)
    {
        if (_directives.Contains(directive))
        {
            return;
        }
        
        _directives.Add(directive);
        AppendLine($"using {directive};");
    }
    
    public void DeclareDirectives(List<string> directives)
    {
        foreach (string directive in directives)
        {
            DeclareDirective(directive);
        }
    }
    
    public void BeginPreproccesorBlock(string condition)
    {
        AppendLine($"#if {condition}");
    }
    
    public void EndPreproccesorBlock()
    {
        AppendLine("#endif");
    }
    
    public void BeginWithEditorPreproccesorBlock()
    {
        BeginPreproccesorBlock("WITH_EDITOR");
    }
    
    public void TryAddWithEditor(UhtProperty property)
    {
        if (property.HasAllFlags(EPropertyFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock();
        }
    }
    
    public void TryEndWithEditor(UhtProperty property)
    {
        if (property.HasAllFlags(EPropertyFlags.EditorOnly))
        {
            EndPreproccesorBlock();
        }
    }
    
    public void TryEndWithEditor(UhtFunction function)
    {
        if (function.FunctionFlags.HasAllFlags(EFunctionFlags.EditorOnly))
        {
            EndPreproccesorBlock();
        }
    }
    
    public void TryAddWithEditor(UhtFunction function)
    {
        if (function.FunctionFlags.HasAllFlags(EFunctionFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock();
        }
    }
    
    public void BeginUnsafeBlock()
    {
        AppendLine("unsafe");
        OpenBrace();
    }
    
    public void EndUnsafeBlock()
    {
        CloseBrace();
    }
    
    public void GenerateTypeSkeleton(string typeNameSpace)
    {
        DeclareDirective(ScriptGeneratorUtilities.EngineNamespace);
        DeclareDirective(ScriptGeneratorUtilities.AttributeNamespace);
        DeclareDirective(ScriptGeneratorUtilities.InteropNamespace);
        DeclareDirective("System.Runtime");
        DeclareDirective("System.Runtime.InteropServices");

        AppendLine();
        AppendLine($"namespace {typeNameSpace};");
        AppendLine();
    }
    
    public void DeclareType(string typeName, string declaredTypeName, string? baseType = null, bool isPartial = true, List<UhtType>? interfaces = default)
    {
        string partialSpecifier = isPartial ? "partial " : string.Empty;
        string baseSpecifier = baseType != null ? $" : {baseType}" : string.Empty;
        string interfacesDeclaration = string.Empty;

        if (interfaces != null)
        {
            foreach (UhtType @interface in interfaces)
            {
                interfacesDeclaration += $", I{@interface.EngineName}";
            }
        }

        AppendLine($"public {partialSpecifier}{typeName} {declaredTypeName}{baseSpecifier} {interfacesDeclaration}");
        OpenBrace();
    }
}