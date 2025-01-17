﻿# Intersection types for C#!
[![NuGet version (IotaLambda.Intersection)](https://img.shields.io/nuget/v/IotaLambda.Intersection.svg?style=flat)](https://www.nuget.org/packages/IotaLambda.Intersection/) [![Build, Test and Deploy to NuGet.org](https://github.com/iotalambda/IotaLambda.Intersection/actions/workflows/main.yml/badge.svg)](https://github.com/iotalambda/IotaLambda.Intersection/actions/workflows/main.yml)

## Installation
Install [IotaLambda.Intersection](https://www.nuget.org/packages/IotaLambda.Intersection/) to each project _containing IntersectionTypes_. This will make a source generator to be executed for those types.
```powershell
Install-Package IotaLambda.Intersection
```

## In a nutshell
Consider the following model:
```csharp
interface IPokemon
{
    string GetNickName();
    void GetHp();
    void TakeDamage(int damage);
}

interface IFireType
{
    void AttackWithFlamethrower(IPokemon enemy);
}

interface IFlyingType
{
    void FlyTo(string city);
}

class Charizard : IPokemon, IFireType, IFlyingType
{
    int hp = 120;
    string location = "Cerulean City";

    public string GetNickName() => "Lizardon";
    public void GetHp() => hp;
    public void TakeDamage(int damage) => hp -= damage;
    public void AttackWithFlamethrower(IPokemon enemy) => enemy.TakeDamage(50);
    public void FlyTo(string city) => location = city;
}
```

You can create intersection types such as:
```csharp
[IntersectionType]
readonly struct SFirePokemon : IPokemon, IFireType; // S as in Shape

[IntersectionType]
readonly struct SFlyingPokemon : IPokemon, IFlyingType;
```

and use them as types as usual without having to colour your codebase with generic constraints like `where TPokemon : IPokemon, IFlyingType`:
```csharp
var ash = new Ash();
var charizard = new Charizard();
ash.Fly_Better(SFlyingPokemon.From(charizard), "Lavender Town");

public class Ash
{
    public bool Fly_Better(SFlyingPokemon pokemon, string city) // No need to define generic constraints, as the intersection type already covers that!
    {
        if (pokemon.GetHp() <= 0)
            return false;

        pokemon.FlyTo(city);
        DescribeTravel("flew", pokemon.AsComponent<IPokemon>(), city); // Use `AsComponent<>` to get one of the components without boxing. `AsComponent<>`s are statically typed overloads for each component!
        return true;
    }

    public bool Fly_Normal<TPokemon>(TPokemon pokemon, string city) where TPokemon : IPokemon, IFlyingPokemon
    {
        if (pokemon.GetHp() <= 0)
            return false;

        pokemon.FlyTo(city);
        DescribeTravel("flew", pokemon, city);
        return true;
    }

    void DescribeTravel(string verb, IPokemon pokemon, string city)
    {
        System.Console.WriteLine($"Ash {verb} to {city} with {pokemon.GetNickName()}.")
    }
}
```