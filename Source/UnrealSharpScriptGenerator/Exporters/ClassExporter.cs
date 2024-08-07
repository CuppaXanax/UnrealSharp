﻿using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Tooltip;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class ClassExporter
{
    public static void ExportClass(UhtClass classObj)
    {
        GeneratorStringBuilder stringBuilder = new();

        string typeNameSpace = classObj.GetNamespace();

        List<UhtProperty> exportedProperties = new();
        ScriptGeneratorUtilities.GetExportedProperties(classObj, ref exportedProperties);
        
        List<UhtFunction> exportedFunctions = new();
        List<UhtFunction> exportedOverrides = new();
        ScriptGeneratorUtilities.GetExportedFunctions(classObj, ref exportedFunctions, ref exportedOverrides);

        List<UhtType> interfaces = new();
        ScriptGeneratorUtilities.GetInterfaces(classObj, ref interfaces);

        List<string> classDependencies = new();
        ScriptGeneratorUtilities.GatherDependencies(classObj, exportedFunctions, exportedOverrides, exportedProperties, interfaces, classDependencies);
        
        stringBuilder.DeclareDirectives(classDependencies);
        stringBuilder.GenerateTypeSkeleton(typeNameSpace);
        
        stringBuilder.AppendTooltip(classObj);
        
        AttributeBuilder attributeBuilder = AttributeBuilder.CreateAttributeBuilder(classObj);
        if (classObj.ClassFlags.HasAnyFlags(EClassFlags.Abstract))
        {
            attributeBuilder.AddArgument("ClassFlags.Abstract");
        }
        attributeBuilder.AddGeneratedTypeAttribute();
        attributeBuilder.Finish();
        stringBuilder.AppendLine(attributeBuilder.ToString());

        string superClassName;
        if (classObj.SuperClass != null)
        {
            superClassName = classObj.SuperClass.GetFullManagedName();
        }
        else
        {
            superClassName = "UnrealSharpObject";
        }
        
        stringBuilder.DeclareType("class", classObj.GetStructName(), superClassName, true, interfaces);
        
        StaticConstructorUtilities.ExportStaticConstructor(stringBuilder, classObj, exportedProperties, exportedFunctions, exportedOverrides);
        
        List<string> reservedNames = GetReservedNames(exportedProperties, exportedOverrides, exportedFunctions);
        ExportClassProperties(stringBuilder, exportedProperties, reservedNames);
        
        ExportClassFunctions(classObj, stringBuilder, exportedFunctions);
        ExportOverrides(stringBuilder, exportedOverrides);
        
        stringBuilder.AppendLine();
        stringBuilder.CloseBrace();
        
        FileExporter.SaveGlueToDisk(classObj, stringBuilder);
    }
    
    public static List<string> GetReservedNames(List<UhtProperty> properties, List<UhtFunction> overrideFunctions, List<UhtFunction> functions)
    {
        List<string> reservedNames = new();
        
        void AddReservedName(string name)
        {
            if (!reservedNames.Contains(name))
            {
                reservedNames.Add(name);
            }
        }
        
        foreach (UhtProperty property in properties)
        {
            AddReservedName(NameMapper.ScriptifyName(property.EngineName, ENameType.Property));
        }
        foreach (UhtFunction function in overrideFunctions)
        {
            AddReservedName(function.GetFunctionName());
        }
        foreach (UhtFunction function in functions)
        {
            AddReservedName(function.GetFunctionName());
        }
        
        return reservedNames;
    }

    static void ExportClassProperties(GeneratorStringBuilder generatorStringBuilder, List<UhtProperty> exportedProperties, List<string> reservedNames)
    {
        foreach (UhtProperty property in exportedProperties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(property)!;
            translator.ExportProperty(generatorStringBuilder, property, reservedNames);
        }
    }
    
    static void ExportOverrides(GeneratorStringBuilder builder, List<UhtFunction> exportedOverrides)
    {
        foreach (UhtFunction function in exportedOverrides)
        {
            FunctionExporter.ExportOverridableFunction(builder, function);
        }
    }
    
    static void ExportClassFunctions(UhtClass owner, GeneratorStringBuilder builder, List<UhtFunction> exportedFunctions)
    {
        bool isBlueprintFunctionLibrary = owner.IsChildOf(Program.BlueprintFunctionLibrary);
        foreach (UhtFunction function in exportedFunctions)
        {
            if (function.HasAllFlags(EFunctionFlags.Static) && isBlueprintFunctionLibrary)
            {
                FunctionExporter.TryAddExtensionMethod(function);
            }
            
            FunctionExporter.ExportFunction(builder, function, FunctionType.Normal);
        }
    }
}