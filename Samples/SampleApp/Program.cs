using IotaLambda.Intersection;
using SampleApp;

{
    var me = new Me();

    Console.WriteLine("LET'S PLAY TENNIS:");
    var sportILike = SSportILike.From(new Tennis());
    me.PlayMyDesiredSport(sportILike);

    Console.WriteLine();
    Console.WriteLine("LET'S PLAY ICEHOCKEY:");
    var skatesSportILike = SSportILike<Skate>.From(new Icehockey());
    me.PlayMyDesiredSport(skatesSportILike);
    me.CheckTheShoes(skatesSportILike);

    // ERROR: The type 'SampleApp.Jogging' cannot be used as type parameter 'T' in the generic type or method 'MyDesiredSportShape.From<T>(T, object)'. There is no implicit reference conversion from 'SampleApp.Jogging' to 'SampleApp.IYouHitAThing'.
    //myDesiredSport = MyDesiredSportShape.From(new Jogging<JoggingShoe>());

    // ERROR: The type 'SampleApp.Badminton' cannot be used as type parameter 'T' in the generic type or method 'MyDesiredSportShape.From<T>(T, object)'. There is no implicit reference conversion from 'SampleApp.Badminton' to 'SampleApp.IOutdoorSport'.
    //myDesiredSport = MyDesiredSportShape.From(new Badminton());

    // ERROR: The type 'SampleApp.Tennis' cannot be used as type parameter 'T' in the generic type or method 'SMyDesiredSportWithShoe<Skate>.From<T>(T, object)'. There is no implicit reference conversion from 'SampleApp.Tennis' to 'SampleApp.IOutdoorSportWithShoe<SampleApp.Skate>'.
    // myDesiredSportWithSkates = SMyDesiredSportWithShoe<Skate>.From(new Tennis());
}

[IntersectionType]
readonly partial struct SSportILike : IOutdoorSport, IYouHitAThing;

[IntersectionType, WithImplicitCast(typeof(SSportILike))]
readonly partial struct SSportILike<TWithShoe> : IOutdoorSport<TWithShoe>, IYouHitAThing where TWithShoe : Shoe;

[IntersectionType, WithImplicitCast(typeof(SSportILike))]
readonly partial struct SSportWithThingsGettingHit : IYouHitAThing;


class Me
{
    public void PlayMyDesiredSport(SSportILike sport)
    {
        if (sport.GetCanBePlayedDuringWinter())
            Console.WriteLine("Yayy snow!");
        else
            Console.WriteLine("Yayy sunshine!");

        // Implicit cast
        //   from [MyDesiredSportShape : IOutdoorSport, IYouHitAThing]
        //   to [SportWithThingsGettingHit : IYouHitAThing].
        PonderAboutTheSport(sport);
    }

    void PonderAboutTheSport(SSportWithThingsGettingHit sport)
    {
        Console.WriteLine(
            $"In this sport we hit a {sport.GetHitThing()}" +
            $" which is{(sport.IsValidHitThingColor(ColorKind.Green) ? "" : " not")}" +
            $" green.");
    }

    public void CheckTheShoes<TShoe>(SSportILike<TShoe> sport) where TShoe : Shoe
    {
        Console.WriteLine($"This sport requires {sport.GetShoe().GetName()}");
    }
}