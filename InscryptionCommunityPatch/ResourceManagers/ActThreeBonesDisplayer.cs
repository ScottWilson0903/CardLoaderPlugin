using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionCommunityPatch.Card;
using Pixelplacement;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InscryptionCommunityPatch.ResourceManagers;

[HarmonyPatch]
public static class Act3BonesDisplayer
{
    public static bool ForceBonesDisplayActive { get; set; } = false;

    public static bool DisplayAct3Bones => ForceBonesDisplayActive || PoolHasBones || PatchPlugin.configAct3Bones.Value;

    private static bool CardIsVisible(this CardInfo info)
    {
        if (info.temple != CardTemple.Tech)
            return false;

        // Now we check metacategories
        // If the card's metacategories are set such that it can't actually appear, don't count it
        return info.metaCategories.Exists((CardMetaCategory x) =>
        x == CardMetaCategory.ChoiceNode || x == CardMetaCategory.Part3Random || x == CardMetaCategory.Rare);
    }

    public static bool PoolHasBones => CardManager.AllCardsCopy.Any(ci => ci.BonesCost > 0 && ci.CardIsVisible());

    public static bool SceneCanHaveBonesDisplayer
    {
        get
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene == null || string.IsNullOrEmpty(activeScene.name))
                return false;

            return activeScene.name.ToLowerInvariant().Contains("part3");
        }
    }

    private static ConditionalWeakTable<ResourceDrone, GameObject> _boneTvScreen = new();
    private static ConditionalWeakTable<ResourceDrone, SpriteRenderer> _boneCountRenderer = new();

    private static GameObject BonesTVScreen
    {
        get
        {
            GameObject tvScreen;
            if (_boneTvScreen.TryGetValue(ResourceDrone.Instance, out tvScreen))
                return tvScreen;
            return null;
        }
        set { _boneTvScreen.Add(ResourceDrone.Instance, value); }
    }

    private static SpriteRenderer BonesCountRenderer
    {
        get
        {
            SpriteRenderer tvScreen;
            if (_boneCountRenderer.TryGetValue(ResourceDrone.Instance, out tvScreen))
                return tvScreen;
            return null;
        }
        set { _boneCountRenderer.Add(ResourceDrone.Instance, value); }
    }


    private static Material _holoRefMaterial;
    private static Material HoloRefMaterial
    {
        get
        {
            _holoRefMaterial ??= CardLoader.GetCardByName("BridgeRailing").holoPortraitPrefab.GetComponentInChildren<Renderer>().material;
            return _holoRefMaterial;
        }
    }

    private static GameObject MakeBoneTokenPrefab(Transform parent)
    {
        GameObject retval = GameObject.Instantiate(ResourceBank.Get<GameObject>("Prefabs/CardBattle/CardBattle").GetComponentInChildren<Part1ResourcesManager>().boneTokenPrefab, parent);

        retval.transform.position = parent.position + Vector3.up * 0.25f;

        GameObject.Destroy(retval.GetComponent<BoneTokenInteractable>());
        //GameObject.Destroy(retval.GetComponent<Rigidbody>());

        Rigidbody rb = retval.GetComponent<Rigidbody>();
        rb.angularVelocity = new Vector3(0, 30, 0);
        rb.maxAngularVelocity = 30;
        rb.inertiaTensor = new Vector3(0.2f, 10.0f, 0.2f);

        foreach (Renderer renderer in retval.GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in renderer.materials)
            {
                material.shader = Shader.Find("SFHologram/HologramShader");
                material.CopyPropertiesFromMaterial(HoloRefMaterial);
                material.SetColor("_Color", Color.white);
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", Color.white);
                if (material.HasProperty("_MainColor"))
                    material.SetColor("_MainColor", Color.white);
                if (material.HasProperty("_RimColor"))
                    material.SetColor("_RimColor", Color.white);
            }
        }

        return retval;

    }

    private static void DisplayBones(int bones)
    {
        if (BonesCountRenderer != null)
        {
            Texture2D bonesCount = Part3CardCostRender.GetIconifiedCostTexture(Part3CardCostRender.BoneCostIcon, bones, true).Item2;
            BonesCountRenderer.sprite = Sprite.Create(bonesCount, new Rect(0f, 0f, bonesCount.width, bonesCount.height), new Vector2(0.5f, 0.5f));
        }
    }

    private static void GlitchOutBolt(GameObject bolt)
    {
        AudioController.Instance.PlaySound3D("factory_chest_open", MixerGroup.TableObjectsSFX, bolt.transform.position, 1f, 0f, new(AudioParams.Pitch.Variation.Medium), null, null, null, false).spatialBlend = 0.25f;

        GlitchOutAssetEffect.TryLoad3DMaterial();
        foreach (Renderer renderer in bolt.GetComponentsInChildren<Renderer>())
        {
            renderer.material = GlitchOutAssetEffect.glitch3DMaterial;
            renderer.material.SetColor("_ASEOutlineColor", Color.white);
        }

        Tween.Shake(bolt.transform, bolt.transform.localPosition, Vector3.one * 0.2f, 0.1f, 0f, Tween.LoopType.Loop, null, null, false);
        CustomCoroutine.WaitThenExecute(0.2f, () => { if (bolt != null) GameObject.Destroy(bolt.gameObject); });
    }

    [HarmonyPatch(typeof(ResourcesManager), nameof(ResourcesManager.ShowAddBones))]
    [HarmonyPostfix]
    private static IEnumerator ShowAddBones_Act3(IEnumerator sequence, int amount, CardSlot slot)
    {
        PatchPlugin.Logger.LogDebug($"Adding {amount} bones");
        if (BonesTVScreen != null && amount > 0)
        {
            int oldBones = ResourcesManager.Instance.PlayerBones - amount;

            for (int i = oldBones; i < oldBones + amount; i++)
            {
                if (slot != null)
                {
                    GameObject bolt = WeightUtil.SpawnWeight(slot.transform, slot.transform.position + Vector3.up, false);
                    CustomCoroutine.WaitThenExecute(1.25f, () => GlitchOutBolt(bolt));
                    yield return new WaitForSeconds(0.1f);
                    AudioController.Instance.PlaySound3D((UnityEngine.Random.value > 0.5f) ? "metal_drop#3" : "metal_object_short#2", MixerGroup.TableObjectsSFX, slot.transform.position, 1f, 0f, new AudioParams.Pitch(AudioParams.Pitch.Variation.Medium), null, null, null, false);
                }

                DisplayBones(i);

                if (slot == null)
                    AudioController.Instance.PlaySound3D("factory_chest_open", MixerGroup.TableObjectsSFX, BonesTVScreen.transform.position, 1f, 0f, new AudioParams.Pitch(0.95f + (float)i * 0.01f), null, null, null, false).spatialBlend = 0.25f;
                yield return new WaitForSeconds(0.05f);
            }
            DisplayBones(ResourcesManager.Instance.PlayerBones);
        }
        else
        {
            yield return sequence;
        }
        yield break;
    }

    [HarmonyPatch(typeof(ResourcesManager), nameof(ResourcesManager.ShowSpendBones))]
    [HarmonyPostfix]
    private static IEnumerator ShowSpendBones_Act3(IEnumerator sequence, int amount)
    {
        PatchPlugin.Logger.LogDebug($"Spending {amount} bones");
        if (BonesTVScreen != null && amount > 0)
        {
            int visualizedDrop = Mathf.Min(20, amount);
            int oldBones = ResourcesManager.Instance.PlayerBones + amount;

            for (int i = oldBones; i > oldBones - visualizedDrop; i--)
            {
                DisplayBones(i);
                AudioController.Instance.PlaySound3D("factory_chest_open", MixerGroup.TableObjectsSFX, BonesTVScreen.transform.position, 0.6f, 0f, new AudioParams.Pitch(0.95f + (float)i * 0.01f), null, null, null, false).spatialBlend = 0.25f;
                yield return new WaitForSeconds(0.05f);
            }
            DisplayBones(ResourcesManager.Instance.PlayerBones);
        }
        else
        {
            yield return sequence;
        }
        yield break;
    }

    [HarmonyPatch(typeof(ResourceDrone), nameof(ResourceDrone.Awake))]
    [HarmonyPostfix]
    private static void AttachBonesTVScreen(ref ResourceDrone __instance)
    {
        if (SceneCanHaveBonesDisplayer && DisplayAct3Bones)
        {
            // Set up the TV screen itself
            BonesTVScreen = GameObject.Instantiate(SpecialNodeHandler.Instance.buildACardSequencer.screen.gameObject, __instance.gameObject.transform.Find("Anim"));
            GameObject.Destroy(BonesTVScreen.transform.Find("Anim/ScreenInteractables").gameObject);
            GameObject.Destroy(BonesTVScreen.transform.Find("RenderCamera/Content/BuildACardInterface").gameObject);
            BonesTVScreen.transform.Find("Anim/CableStart_1").gameObject.SetActive(false);
            BonesTVScreen.transform.Find("Anim/CableStart_2").gameObject.SetActive(false);
            BonesTVScreen.transform.Find("Anim/CableStart_3").gameObject.SetActive(false);

            BonesTVScreen.transform.localScale = new(0.3f, 0.2f, 0.2f);

            BonesTVScreen.transform.localPosition = new(0.6f, 0.2f, -1.8f); // below gems module
            //BonesTVScreen.transform.localPosition = new (-1.1717f, 0.22f, -0.85f); // Right side of thingo
            BonesTVScreen.transform.localEulerAngles = new(270f, 180f, 0f);

            // Make the TV frame and arm glow juuuuust a little bit to be more visible
            GameObject frame = BonesTVScreen.transform.Find("Anim/BasicScreen/Frame").gameObject;
            Renderer frameRenderer = frame.GetComponent<Renderer>();
            frameRenderer.material.EnableKeyword("_EMISSION");
            frameRenderer.material.SetColor("_EmissionColor", new(0f, 0.085f, 0.085f, 1.0f));

            GameObject pole = BonesTVScreen.transform.Find("Anim/Pole").gameObject;
            Renderer poleRenderer = pole.GetComponent<Renderer>();
            poleRenderer.material.EnableKeyword("_EMISSION");
            poleRenderer.material.SetColor("_EmissionColor", new(0f, 0.1f, 0.1f, 1.0f));
            poleRenderer.material.SetTexture("_EmissionMap", Texture2D.whiteTexture);

            BonesTVScreen.SetActive(true);
            BonesTVScreen.name = "Module-Bones";

            // Put a texture renderer on the TV screen that we can update with the appropriate textures:
            GameObject boneCountTexture = new GameObject("BoneCounter");
            boneCountTexture.layer = LayerMask.NameToLayer("CardOffscreen");
            boneCountTexture.transform.SetParent(BonesTVScreen.transform.Find("RenderCamera/Content"));
            boneCountTexture.transform.localPosition = new Vector3(-5f, -2.78f, 7.0601f);
            boneCountTexture.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            boneCountTexture.transform.localScale = new Vector3(18f, 33f, 10f);

            BonesCountRenderer = boneCountTexture.AddComponent<SpriteRenderer>();
            BonesCountRenderer.SetMaterial(Resources.Load<Material>("art/materials/sprite_coloroverlay"));
            BonesCountRenderer.material.SetColor("_Color", GameColors.Instance.brightBlue * 0.85f);

            BonesCountRenderer.material.EnableKeyword("_EMISSION");
            BonesCountRenderer.material.SetColor("_EmissionColor", Color.white);

            // Set the TV settings
            OLDTVScreen tvScreen = BonesTVScreen.GetComponentInChildren<OLDTVScreen>();
            tvScreen.chromaticAberrationMagnetude = 0.1f;
            tvScreen.noiseMagnetude = 0.2f;
            tvScreen.staticMagnetude = 0.2f;
            tvScreen.staticVertical = 15;
            tvScreen.staticVerticalScroll = 0.1f;
            tvScreen.screenSaturation = 0f;

            OLDTVTube tvTube = BonesTVScreen.GetComponentInChildren<OLDTVTube>();
            tvTube.radialDistortion = 0.6f;
            tvTube.reflexMagnetude = 0.2f;

            // Move the behind-the-scenes render camera out of the way of P03's Face:
            BonesTVScreen.transform.Find("RenderCamera").transform.position = new(0f, -30f, -30f);

            DisplayBones(0);

            boneCountTexture.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(Part3ResourcesManager), nameof(Part3ResourcesManager.CleanUp))]
    [HarmonyPrefix]
    private static void CleanUpPart3Bones(Part3ResourcesManager __instance)
    {
        __instance.PlayerBones = 0;
        DisplayBones(0);
    }
}