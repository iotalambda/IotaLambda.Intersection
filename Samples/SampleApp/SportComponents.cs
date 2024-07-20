namespace SampleApp;

interface IPlayedWithRackets
{
    decimal GetRacketMinAllowedLengthMm();
    decimal GetRacketMaxAllowedLengthMm();
}

enum HitThingKind { Puck, Ball, Shuttlecock }
enum ColorKind { Yellow, White, Green, Black }

interface IYouHitAThing
{
    HitThingKind GetHitThing();
    bool IsValidHitThingColor(ColorKind color);
}

interface IIndoorSport;

interface IOutdoorSport
{
    bool GetCanBePlayedDuringWinter();
}

interface IOutdoorSport<TShoe> : IOutdoorSport where TShoe : Shoe
{
    TShoe GetShoe();
}