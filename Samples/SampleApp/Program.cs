using IotaLambda.Intersection;
using SampleApp;

{
    var me = new Me();

    Console.WriteLine("LET'S PLAY TENNIS:");
    var myDesiredSport = MyDesiredSportShape.From(new Tennis());
    me.PlayMyDesiredSport(myDesiredSport);

    Console.WriteLine();
    Console.WriteLine("LET'S PLAY ICEHOCKEY:");
    myDesiredSport = MyDesiredSportShape.From(new Icehockey());
    me.PlayMyDesiredSport(myDesiredSport);

    // ERROR: The type 'SampleApp.Jogging' cannot be used as type parameter 'T' in the generic type or method 'MyDesiredSportShape.From<T>(T, object)'. There is no implicit reference conversion from 'SampleApp.Jogging' to 'SampleApp.IYouHitAThing'.
    //myDesiredSport = MyDesiredSportShape.From(new Jogging());

    // ERROR: The type 'SampleApp.Badminton' cannot be used as type parameter 'T' in the generic type or method 'MyDesiredSportShape.From<T>(T, object)'. There is no implicit reference conversion from 'SampleApp.Badminton' to 'SampleApp.IOutdoorSport'.
    //myDesiredSport = MyDesiredSportShape.From(new Badminton());

}


[IntersectionType]
readonly partial struct MyDesiredSportShape : IOutdoorSport, IYouHitAThing;


[IntersectionType, WithImplicitCast(typeof(MyDesiredSportShape))]
readonly partial struct SportWithThingsGettingHit : IYouHitAThing;


class Me
{
    public void PlayMyDesiredSport(MyDesiredSportShape sport)
    {
        if (sport.GetCanBePlayedDuringWinter())
            Console.WriteLine("Yayy snow!");
        else
            Console.WriteLine("Yayy sunshine!");

        // Implicit cast
        //   from [MyDesiredSportShape : IOutdoorSport, IYouHitAThing]
        //   to [SportWithThingsGettingHit : IYouHitAThing].
        PonderAboutThingGettingHit(sport);
    }

    void PonderAboutThingGettingHit(SportWithThingsGettingHit sport)
    {
        Console.WriteLine(
            $"In this sport we hit a {sport.GetHitThing()}" +
            $" which is{(sport.IsValidHitThingColor(ColorKind.Green) ? "" : " not")}" +
            $" green.");
    }
}