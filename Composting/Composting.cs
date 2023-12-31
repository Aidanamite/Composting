using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using HMLLibrary;

public class Composting : Mod
{
    public static Item_Base Ash;
    public static List<AshRecipe> ashRecipes = new List<AshRecipe>()
    {
        new AshRecipe(ItemManager.GetItemByName("Cooked_GenericMeat"), 2, new Vector3(-0.03f, 0.85f, -0.145f), new Vector3(60, 30, 0)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Drumstick"), 1, new Vector3(-0.09f, 0.765f, -0.15f), new Vector3(150, 30, 0)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Shark"), 2, new Vector3(-0.079f, 0.84f, -0.14f), new Vector3(60, 30, 0)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Mackerel"), 1, new Vector3(-0.079f, 0.84f, -0.14f), new Vector3(60, 30, 90)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Salmon"), 2, new Vector3(-0.07f, 0.95f, -0.11f), new Vector3(40, 30, 90)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Catfish"), 2, new Vector3(-0.04f, 1.02f, -0.11f), new Vector3(60, 30, 110)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Pomfret"), 1, new Vector3(0, 0.69f, 0), new Vector3(0, 30, 90)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Tilapia"), 2, new Vector3(-0.07f, 0.82f, -0.135f), new Vector3(60, 30, 90)),
        new AshRecipe(ItemManager.GetItemByName("Cooked_Herring"), 1, new Vector3(-0.079f, 0.775f, -0.14f), new Vector3(60, 30, 90))
    };
    Harmony harmony;
    public void Start()
    {
        var Dirt = ItemManager.GetItemByName("Dirt");
        Ash = Dirt.Clone(12345, "Ash");
        Ash.settings_Inventory.LocalizationTerm = "";
        Ash.settings_Inventory.DisplayName = "Ash";
        Ash.settings_Inventory.Description = "Very cooked meat.";
        Ash.settings_Inventory.Sprite = Ash.settings_Inventory.Sprite.GetReadable();
        Color[] image = Ash.settings_Inventory.Sprite.texture.GetPixels();
        for (int i = 0; i < image.Length; i++)
            image[i] = image[i].WhiteShiftedGrayscale();
        Ash.settings_Inventory.Sprite.texture.SetPixels(image);
        Ash.settings_Inventory.Sprite.texture.Apply();
        RAPI.RegisterItem(Ash);

        foreach (AshRecipe recipe in ashRecipes)
            recipe.Item.settings_cookable = new ItemInstance_Cookable(1, ItemManager.GetItemByName(recipe.Item.UniqueName.Replace("Cooked_", "Raw_")).settings_cookable.CookingTime, new Cost(Ash, recipe.Amount));

        Dirt.SetRecipe(new CostMultiple[] {
            new CostMultiple(new Item_Base[] { Ash }, 5),
            new CostMultiple(new Item_Base[] {
                ItemManager.GetItemByName("VineGoo"),
                ItemManager.GetItemByName("CaveMushroom"),
                ItemManager.GetItemByName("SilverAlgae")
            }, 1)
        }, amountToCraft: 5);

        harmony = new Harmony("com.aidanamite.NewItemTest");
        harmony.PatchAll();

        Log("Mod has been loaded!");
    }

    public static void ModifySlot(CookingSlot slot)
    {
        var connections = Traverse.Create(slot).Field<List<CookItemConnection>>("itemConnections");
        foreach (CookItemConnection connection in connections.Value)
            if (connection.cookableItem.UniqueName == "MetalOre")
                goto init;
        return;
    init:
        GameObject[] cookedMeats = new GameObject[ashRecipes.Count];
        GameObject sand = null;
        foreach (MeshRenderer r in Resources.FindObjectsOfTypeAll<MeshRenderer>())
            if (r.name == "Raw_Sand")
                sand = r.gameObject;
            else if (r.transform.parent != null && r.transform.parent.parent != null && r.transform.parent.parent.name.Contains("CookingSlot"))
                for (int i = 0; i < ashRecipes.Count; i++)
                    if (cookedMeats[i] == null && r.name == ashRecipes[i].Item.UniqueName)
                        cookedMeats[i] = r.gameObject;
        var ash = new GameObject("Ash");
        ash.SetActive(false);
        var cookedMesh = ash.AddComponent<MeshRenderer>();
        cookedMesh.material = GameObject.Instantiate<Material>(sand.GetComponent<MeshRenderer>().material);
        cookedMesh.material.mainTexture = (sand.GetComponent<MeshRenderer>().material.mainTexture as Texture2D).GetReadable();
        Color[] image = (cookedMesh.material.mainTexture as Texture2D).GetPixels();
        for (int i = 0; i < image.Length; i++)
            image[i] = image[i].Grayscale().Lightness(0.2f);
        (cookedMesh.material.mainTexture as Texture2D).SetPixels(image);
        (cookedMesh.material.mainTexture as Texture2D).Apply();
        ash.AddComponent<MeshFilter>().mesh = sand.GetComponent<MeshFilter>().mesh;
        ash.transform.SetParent(connections.Value[0].cookedItem.transform.parent, false);
        ash.transform.localPosition = sand.transform.localPosition;
        ash.transform.localRotation = sand.transform.localRotation;
        ash.transform.localScale = sand.transform.localScale;

        var newRecipes = new CookItemConnection[ashRecipes.Count];
        for (int i = 0; i < ashRecipes.Count; i++)
        {
            newRecipes[i] = new CookItemConnection()
            {
                cookableItem = ashRecipes[i].Item,
                rawItem = new GameObject(ashRecipes[i].Item.UniqueName),
                cookedItem = ash,
                name = "Ash_" + ashRecipes[i].Item.UniqueName
            };

            newRecipes[i].rawItem.SetActive(false);
            newRecipes[i].rawItem.AddComponent<MeshRenderer>().material = cookedMeats[i].GetComponent<MeshRenderer>().material;
            newRecipes[i].rawItem.AddComponent<MeshFilter>().mesh = cookedMeats[i].GetComponent<MeshFilter>().mesh;
            newRecipes[i].rawItem.transform.SetParent(connections.Value[0].rawItem.transform.parent, false);
            newRecipes[i].rawItem.transform.localPosition = ashRecipes[i].Position;
            newRecipes[i].rawItem.transform.localRotation = ashRecipes[i].Rotation;
            newRecipes[i].rawItem.transform.scale(cookedMeats[i].transform.scale());
        }

        connections.Value.AddRange(newRecipes);
    }
}

static class ExtentionMethods
{
    public static Item_Base Clone(this Item_Base source, int uniqueIndex, string uniqueName)
    {
        Item_Base item = ScriptableObject.CreateInstance<Item_Base>();
        item.Initialize(uniqueIndex, uniqueName, source.MaxUses);
        item.settings_buildable = source.settings_buildable.Clone();
        item.settings_consumeable = source.settings_consumeable.Clone();
        item.settings_cookable = source.settings_cookable.Clone();
        item.settings_equipment = source.settings_equipment.Clone();
        item.settings_Inventory = source.settings_Inventory.Clone();
        item.settings_recipe = source.settings_recipe.Clone();
        item.settings_usable = source.settings_usable.Clone();
        return item;
    }

    public static Sprite GetReadable(this Sprite source)
    {
        return Sprite.Create(source.texture.GetReadable(), source.rect, source.pivot, source.pixelsPerUnit);
    }

    public static Texture2D GetReadable(this Texture2D source)
    {
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, temp);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = temp;
        Texture2D texture = new Texture2D(source.width, source.height);
        texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        texture.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(temp);
        return texture;
    }

    public static Color Grayscale(this Color source)
    {
        return new Color(source.grayscale, source.grayscale, source.grayscale, source.a);
    }

    public static Color Lightness(this Color source, float value)
    {
        if (value > 1)
            value = 1;
        if (value < 0)
            value = 0;
        return new Color(source.r * value, source.g * value, source.b * value, source.a);
    }
    public static Color WhiteShiftedGrayscale(this Color source)
    {
        float c = Mathf.Sin(source.grayscale * Mathf.PI / 2);
        return new Color(c, c, c, source.a);
    }

    public static Vector3 scale(this Transform transform)
    {
        if (transform.parent != null)
            return multiplyVector3(transform.parent.scale(), transform.localScale);
        return transform.localScale;
    }

    public static void scale(this Transform transform, Vector3 value)
    {
        if (transform.parent != null)
            value = divideVector3(value, transform.parent.scale());
        transform.localScale = value;
    }

    static Vector3 multiplyVector3(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    static Vector3 divideVector3(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }

    public static void SetRecipe(this ItemInstance_Recipe item, CostMultiple[] cost, CraftingCategory category = CraftingCategory.Resources, int amountToCraft = 1, bool learnedFromBeginning = false)
    {
        Traverse recipe = Traverse.Create(item);
        recipe.Field("craftingCategory").SetValue(category);
        recipe.Field("amountToCraft").SetValue(amountToCraft);
        recipe.Field("learnedFromBeginning").SetValue(learnedFromBeginning);
        item.NewCost = cost;
    }

    public static void SetRecipe(this Item_Base item, CostMultiple[] cost, CraftingCategory category = CraftingCategory.Resources, int amountToCraft = 1, bool learnedFromBeginning = false)
        => item.settings_recipe.SetRecipe(cost, category, amountToCraft, learnedFromBeginning);
}

[HarmonyPatch(typeof(CookingSlot), "Awake")]
class CookingSlot_Create
{
    static void Prefix(CookingSlot __instance)
    {
        Composting.ModifySlot(__instance);
    }
}

public struct AshRecipe
{
    public Item_Base Item { private set; get; }
    public Vector3 Position { private set; get; }
    public Quaternion Rotation { private set; get; }
    public int Amount { private set; get; }

    public AshRecipe(Item_Base item, int amount, Vector3 position, Quaternion rotation)
    {
        Item = item;
        Amount = amount;
        Position = position;
        Rotation = rotation;
    }
    public AshRecipe(Item_Base item, int amount, Vector3 position, Vector3 rotation) : this(item, amount, position, Quaternion.Euler(rotation)) { }
}