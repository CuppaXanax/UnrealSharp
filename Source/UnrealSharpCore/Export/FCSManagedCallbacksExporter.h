#pragma once

#include "CoreMinimal.h"
#include "CSManagedCallbacksCache.h"
#include "UnrealSharpBinds.h"
#include "UObject/Object.h"
#include "FCSManagedCallbacksExporter.generated.h"

class FCSManagedCallbacks;

UCLASS()
class UNREALSHARPCORE_API UFCSManagedCallbacksExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static FCSManagedCallbacks::FManagedCallbacks* GetManagedCallbacks();
};
