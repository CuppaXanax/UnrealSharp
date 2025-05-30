﻿#include "UnrealSharpCompiler.h"

#include "BlueprintCompilationManager.h"
#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "CSManager.h"
#include "KismetCompiler.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSEnum.h"
#include "TypeGenerator/CSScriptStruct.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCompilerModule"

void FUnrealSharpCompilerModule::StartupModule()
{
	UCSManager& CSManager = UCSManager::GetOrCreate();
	
	FKismetCompilerContext::RegisterCompilerForBP(UCSBlueprint::StaticClass(), [](UBlueprint* InBlueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompileOptions)
	{
		return MakeShared<FCSCompilerContext>(CastChecked<UCSBlueprint>(InBlueprint), InMessageLog, InCompileOptions);
	});
	
	IKismetCompilerInterface& KismetCompilerModule = FModuleManager::LoadModuleChecked<IKismetCompilerInterface>("KismetCompiler");
	KismetCompilerModule.GetCompilers().Add(&BlueprintCompiler);
	
	CSManager.OnNewClassEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewClass);
	CSManager.OnNewEnumEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewEnum);
	CSManager.OnNewStructEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewStruct);
	
	CSManager.OnProcessedPendingClassesEvent().AddRaw(this, &FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints);
	CSManager.OnManagedAssemblyLoadedEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnManagedAssemblyLoaded);

	// Try to recompile and reinstance all blueprints when the module is loaded.
	CSManager.ForEachManagedPackage([this](const UPackage* Package)
	{
		ForEachObjectWithPackage(Package, [this](UObject* Object)
		{
			if (UBlueprint* Blueprint = Cast<UBlueprint>(Object))
			{
				OnNewClass(static_cast<UCSClass*>(Blueprint->GeneratedClass.Get()));
			}
			return true;
		}, false);
	});
	
	RecompileAndReinstanceBlueprints();
}

void FUnrealSharpCompilerModule::ShutdownModule()
{
    
}

void FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints()
{
	if (UCSManager::Get().IsLoadingAnyAssembly())
	{
		// Wait until all assemblies are loaded, so we can recompile all blueprints at once.
		return;
	}
	
	if (ManagedComponentsToCompile.IsEmpty() && ManagedClassesToCompile.IsEmpty())
	{
		// Nothing to compile.
		return;
	}
	
	auto QueueAndCompile = [this](TArray<UBlueprint*>& Blueprints)
	{
		if (Blueprints.IsEmpty())
		{
			return;
		}
		
		for (UBlueprint* Blueprint : Blueprints)
		{
			FKismetEditorUtilities::CompileBlueprint(Blueprint, EBlueprintCompileOptions::SkipGarbageCollection);
		}
		
		Blueprints.Reset();
	};

	// Components needs be compiled first, as they are instantiated by the owning actor, and needs their size to be known.
	QueueAndCompile(ManagedComponentsToCompile);
	QueueAndCompile(ManagedClassesToCompile);

	CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS, true);
}

void FUnrealSharpCompilerModule::AddManagedReferences(FCSManagedReferencesCollection& Collection)
{
	Collection.ForEachManagedReference([&](UStruct* Struct)
	{
		if (UCSClass* Class = Cast<UCSClass>(Struct))
		{
			OnNewClass(Class);
		}
	});
}

void FUnrealSharpCompilerModule::OnNewClass(UCSClass* NewClass)
{
	UBlueprint* Blueprint = Cast<UBlueprint>(NewClass->ClassGeneratedBy);
	if (!IsValid(Blueprint))
	{
		return;
	}

	auto AddToCompileList = [this](TArray<UBlueprint*>& List, UBlueprint* Blueprint)
	{
		if (List.Contains(Blueprint))
		{
			return;
		}

		List.Add(Blueprint);
	};
		
	if (NewClass->IsChildOf(UActorComponent::StaticClass()))
	{
		AddToCompileList(ManagedComponentsToCompile, Blueprint);
	}
	else
	{
		AddToCompileList(ManagedClassesToCompile, Blueprint);
	}
}

void FUnrealSharpCompilerModule::OnNewStruct(UCSScriptStruct* NewStruct)
{
	AddManagedReferences(NewStruct->ManagedReferences);
}

void FUnrealSharpCompilerModule::OnNewEnum(UCSEnum* NewEnum)
{
	AddManagedReferences(NewEnum->ManagedReferences);
}

void FUnrealSharpCompilerModule::OnManagedAssemblyLoaded(const FName& AssemblyName)
{
	RecompileAndReinstanceBlueprints();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)