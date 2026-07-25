using Microsoft.EntityFrameworkCore;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;
using RecipeBook.Infrastructure.Persistence;

namespace RecipeBook.Api.Seeding;

public static class DbSeeder
{
    private const string DemoOwnerId = "demo-user-001";

    public static async Task SeedAsync(RecipeBookDbContext db)
    {
        if (await db.Recipes.AnyAsync())
            return; // Already seeded.

        var recipes = new[]
        {
            CreateBreakfast(),
            CreateLunch(),
            CreateDinner(),
            CreateSnack(),
            CreateDessert(),
            CreatePrivateRecipe()
        };

        foreach (var recipe in recipes)
            db.Recipes.Add(recipe);

        await db.SaveChangesAsync();
    }

    private static Recipe CreateBreakfast()
    {
        var r = new Recipe(
            title: "Fluffy Blueberry Pancakes",
            ownerId: DemoOwnerId,
            servingSize: 4,
            category: RecipeCategory.Breakfast,
            description: "Light and fluffy pancakes studded with fresh blueberries. A weekend breakfast classic.",
            imageUrl: "https://images.unsplash.com/photo-1567620913717-ff44acf7e185",
            prepTimeMinutes: 10,
            cookTimeMinutes: 15,
            visibility: RecipeVisibility.Public,
            caloriesPerServing: 320m,
            proteinGrams: 8m,
            carbsGrams: 45m,
            fatGrams: 12m);

        r.AddIngredient("All-purpose flour", 2, "cups");
        r.AddIngredient("Sugar", 3, "tbsp");
        r.AddIngredient("Baking powder", 1, "tbsp");
        r.AddIngredient("Salt", 0.5m, "tsp");
        r.AddIngredient("Milk", 1.5m, "cups");
        r.AddIngredient("Egg", 2, null);
        r.AddIngredient("Melted butter", 4, "tbsp");
        r.AddIngredient("Fresh blueberries", 1.5m, "cups");

        r.AddStep("Whisk together flour, sugar, baking powder, and salt in a large bowl.");
        r.AddStep("In a separate bowl, combine milk, egg, and melted butter. Pour into dry ingredients and stir until just combined (lumps are fine). Fold in blueberries.");
        r.AddStep("Heat a non-stick pan over medium heat. Pour 1/4 cup batter per pancake. Cook until bubbles form on top, then flip and cook 2 more minutes.");
        r.AddStep("Serve warm with maple syrup and extra blueberries.");

        return r;
    }

    private static Recipe CreateLunch()
    {
        var r = new Recipe(
            title: "Mediterranean Quinoa Salad",
            ownerId: DemoOwnerId,
            servingSize: 3,
            category: RecipeCategory.Lunch,
            description: "A vibrant, protein-packed salad with quinoa, cucumbers, tomatoes, feta, and a lemon-herb dressing.",
            imageUrl: "https://images.unsplash.com/photo-1512621776951-a57141f2eefd",
            prepTimeMinutes: 20,
            cookTimeMinutes: 15,
            visibility: RecipeVisibility.Public,
            caloriesPerServing: 380m,
            proteinGrams: 14m,
            carbsGrams: 48m,
            fatGrams: 16m);

        r.AddIngredient("Quinoa", 1.5m, "cups");
        r.AddIngredient("Cucumber", 1, "large, diced");
        r.AddIngredient("Cherry tomatoes", 2, "cups, halved");
        r.AddIngredient("Red onion", 0.5m, "medium, finely chopped");
        r.AddIngredient("Feta cheese", 0.75m, "cup, crumbled");
        r.AddIngredient("Kalamata olives", 0.5m, "cup, sliced");
        r.AddIngredient("Olive oil", 3, "tbsp");
        r.AddIngredient("Lemon juice", 2, "tbsp");
        r.AddIngredient("Dried oregano", 1, "tsp");

        r.AddStep("Rinse quinoa thoroughly. Cook in 3 cups water for 15 minutes until fluffy. Let cool completely.");
        r.AddStep("Whisk olive oil, lemon juice, oregano, salt, and pepper for the dressing.");
        r.AddStep("Toss cooled quinoa with cucumber, tomatoes, red onion, olives, and feta. Drizzle with dressing and toss to combine.");
        r.AddStep("Refrigerate 30 minutes before serving to let flavors meld.");

        return r;
    }

    private static Recipe CreateDinner()
    {
        var r = new Recipe(
            title: "Garlic Herb Roasted Chicken",
            ownerId: DemoOwnerId,
            servingSize: 6,
            category: RecipeCategory.Dinner,
            description: "Juicy whole roasted chicken with crispy skin, infused with garlic, rosemary, and thyme.",
            imageUrl: "https://images.unsplash.com/photo-1598103442097-8b74394b95c6",
            prepTimeMinutes: 20,
            cookTimeMinutes: 90,
            visibility: RecipeVisibility.Public,
            caloriesPerServing: 450m,
            proteinGrams: 42m,
            carbsGrams: 3m,
            fatGrams: 28m);

        r.AddIngredient("Whole chicken", 4.5m, "lbs");
        r.AddIngredient("Olive oil", 3, "tbsp");
        r.AddIngredient("Garlic cloves", 6, "minced");
        r.AddIngredient("Fresh rosemary", 4, "sprigs");
        r.AddIngredient("Fresh thyme", 6, "sprigs");
        r.AddIngredient("Lemon", 1, "halved");
        r.AddIngredient("Butter", 2, "tbsp, softened");
        r.AddIngredient("Salt", 1.5m, "tsp");
        r.AddIngredient("Black pepper", 0.5m, "tsp");

        r.AddStep("Preheat oven to 425°F (220°C). Pat chicken completely dry inside and out.");
        r.AddStep("Mix softened butter with minced garlic, chopped rosemary, thyme leaves, salt, and pepper. Rub mixture all over the chicken, including under the skin.");
        r.AddStep("Tuck wings behind the back. Stuff cavity with lemon halves and remaining herb sprigs. Tie legs together with kitchen twine.");
        r.AddStep("Roast for 75-90 minutes until internal temperature reaches 165°F (74°C) at the thickest part of the thigh. Rest 15 minutes before carving.");

        return r;
    }

    private static Recipe CreateSnack()
    {
        var r = new Recipe(
            title: "Spicy Roasted Chickpeas",
            ownerId: DemoOwnerId,
            servingSize: 2,
            category: RecipeCategory.Snack,
            description: "Crispy, seasoned chickpeas that make a perfect high-protein snack. Customizable heat level.",
            prepTimeMinutes: 5,
            cookTimeMinutes: 30,
            visibility: RecipeVisibility.Public);

        r.AddIngredient("Chickpeas (canned)", 2, "cans, drained and rinsed");
        r.AddIngredient("Olive oil", 2, "tbsp");
        r.AddIngredient("Smoked paprika", 1, "tsp");
        r.AddIngredient("Cumin", 0.5m, "tsp");
        r.AddIngredient("Chili flakes", 0.25m, "tsp");
        r.AddIngredient("Salt", 0.5m, "tsp");

        r.AddStep("Preheat oven to 400°F (200°C). Pat chickpeas very dry with a towel — moisture is the enemy of crispiness.");
        r.AddStep("Toss chickpeas with olive oil and all spices until evenly coated.");
        r.AddStep("Spread in a single layer on a baking sheet. Roast 25-30 minutes, shaking the pan every 10 minutes, until golden and crispy.");
        r.AddStep("Let cool completely — they'll crisp up more as they cool. Store in an airtight container for up to 3 days.");

        return r;
    }

    private static Recipe CreateDessert()
    {
        var r = new Recipe(
            title: "Classic Chocolate Lava Cake",
            ownerId: DemoOwnerId,
            servingSize: 2,
            category: RecipeCategory.Dessert,
            description: "Rich individual chocolate cakes with a molten center. Impressive yet surprisingly simple.",
            imageUrl: "https://images.unsplash.com/photo-1606313564200-e75d5e30476c",
            prepTimeMinutes: 15,
            cookTimeMinutes: 12,
            visibility: RecipeVisibility.Public,
            caloriesPerServing: 480m,
            proteinGrams: 6m,
            carbsGrams: 52m,
            fatGrams: 28m);

        r.AddIngredient("Dark chocolate (70%)", 4, "oz, chopped");
        r.AddIngredient("Butter", 6, "tbsp");
        r.AddIngredient("Eggs", 2, null);
        r.AddIngredient("Egg yolks", 2, null);
        r.AddIngredient("Sugar", 0.5m, "cup");
        r.AddIngredient("All-purpose flour", 2, "tbsp");

        r.AddStep("Preheat oven to 425°F (220°C). Butter and cocoa-dust two 6-oz ramekins.");
        r.AddStep("Melt chocolate and butter together (microwave or double boiler). Whisk in eggs, egg yolks, and sugar until thick and pale.");
        r.AddStep("Gently fold in flour. Divide batter between ramekins.");
        r.AddStep("Bake exactly 12 minutes — edges should be set but center still jiggly. Invert onto plates immediately. Serve with whipped cream.");

        return r;
    }

    private static Recipe CreatePrivateRecipe()
    {
        var r = new Recipe(
            title: "Grandma's Secret Apple Pie",
            ownerId: DemoOwnerId,
            servingSize: 8,
            category: RecipeCategory.Dessert,
            description: "A family recipe passed down through generations. The secret ingredient makes all the difference.",
            prepTimeMinutes: 45,
            cookTimeMinutes: 60,
            visibility: RecipeVisibility.Private);

        r.AddIngredient("Granny Smith apples", 6, "large, peeled and sliced");
        r.AddIngredient("Sugar", 0.75m, "cup");
        r.AddIngredient("Cinnamon", 1.5m, "tsp");
        r.AddIngredient("Nutmeg", 0.25m, "tsp");
        r.AddIngredient("Lemon juice", 1, "tbsp");
        r.AddIngredient("Butter", 3, "tbsp, cubed");
        r.AddIngredient("Pie crust (store-bought or homemade)", 2, null);

        r.AddStep("Toss sliced apples with sugar, cinnamon, nutmeg, and lemon juice. Let sit 15 minutes.");
        r.AddStep("Line a 9-inch pie dish with bottom crust. Fill with apple mixture and dot with butter cubes.");
        r.AddStep("Cover with top crust, seal edges, and cut slits for steam vents. Brush with egg wash.");
        r.AddStep("Bake at 375°F (190°C) for 50-60 minutes until crust is golden brown and filling is bubbling.");

        return r;
    }
}
