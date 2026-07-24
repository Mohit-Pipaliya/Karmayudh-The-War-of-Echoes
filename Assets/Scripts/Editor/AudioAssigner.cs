using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AudioAssigner : EditorWindow
{
    [MenuItem("Tools/Auto Assign All Audio")]
    public static void AssignAudio()
    {
        Debug.Log("Starting Auto Audio Assignment...");
        
        // Find all audio clips in Assets/Sound
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sound" });
        Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                clips[clip.name] = clip;
            }
        }

        // Helper function to safely get a clip or return null with warning
        AudioClip GetClip(string name)
        {
            if (clips.TryGetValue(name, out AudioClip c)) return c;
            Debug.LogWarning("Missing sound file: " + name);
            return null;
        }

        // Setup arrays for categorized sounds
        AudioClip[] playerFootsteps = new[] { GetClip("Great_Footsteps"), GetClip("WalkOnGrass") };
        AudioClip playerJump = GetClip("Jump");
        AudioClip[] playerLandings = new[] { GetClip("Landing"), GetClip("Landing2") };
        AudioClip[] playerAttacks = new[] { GetClip("Slash"), GetClip("Swipe"), GetClip("Sword Unsheathing") };
        AudioClip[] playerDamages = new[] { GetClip("Damage"), GetClip("hitting flesh") };
        AudioClip playerDeath = GetClip("Death");

        AudioClip[] enemyFootsteps = new[] { GetClip("EnemyFootstep") };
        AudioClip[] enemyAttacks = new[] { GetClip("Monster Attack"), GetClip("Monster Jump attack") };
        AudioClip[] enemyDamages = new[] { GetClip("Monster Impact"), GetClip("Impact") };
        AudioClip enemyDeath = GetClip("Monster Collapse"); // Or "Enemy fall down"
        AudioClip enemyGrowl = GetClip("Monster Growl");

        // Find Player
        PlayerController[] players = FindObjectsOfType<PlayerController>(true);
        foreach (PlayerController p in players)
        {
            p.footstepSounds = CleanArray(playerFootsteps);
            p.jumpSound = playerJump;
            p.landingSounds = CleanArray(playerLandings);
            p.attackSounds = CleanArray(playerAttacks);
            p.damageSounds = CleanArray(playerDamages);
            p.deathSound = playerDeath;
            
            EditorUtility.SetDirty(p);
            Debug.Log("Assigned audio to Player: " + p.gameObject.name);
        }

        // Find EnemyAI
        EnemyAI[] enemies1 = FindObjectsOfType<EnemyAI>(true);
        foreach (EnemyAI e in enemies1)
        {
            e.footstepSounds = CleanArray(enemyFootsteps);
            e.attackSounds = CleanArray(enemyAttacks);
            e.damageSounds = CleanArray(enemyDamages);
            e.deathSound = enemyDeath;
            e.growlSound = enemyGrowl;
            EditorUtility.SetDirty(e);
        }
        
        EnemyAI2[] enemies2 = FindObjectsOfType<EnemyAI2>(true);
        foreach (EnemyAI2 e in enemies2)
        {
            e.footstepSounds = CleanArray(enemyFootsteps);
            e.attackSounds = CleanArray(enemyAttacks);
            e.damageSounds = CleanArray(enemyDamages);
            e.deathSound = enemyDeath;
            e.growlSound = enemyGrowl;
            EditorUtility.SetDirty(e);
        }

        EnemyAI3[] enemies3 = FindObjectsOfType<EnemyAI3>(true);
        foreach (EnemyAI3 e in enemies3)
        {
            e.footstepSounds = CleanArray(enemyFootsteps);
            e.attackSounds = CleanArray(enemyAttacks);
            e.damageSounds = CleanArray(enemyDamages);
            e.deathSound = enemyDeath;
            e.growlSound = enemyGrowl;
            EditorUtility.SetDirty(e);
        }

        Debug.Log($"Assigned audio to {enemies1.Length + enemies2.Length + enemies3.Length} enemies.");
        Debug.Log("Audio Assignment Complete!");
    }

    private static AudioClip[] CleanArray(AudioClip[] array)
    {
        List<AudioClip> clean = new List<AudioClip>();
        foreach (var c in array)
        {
            if (c != null) clean.Add(c);
        }
        return clean.ToArray();
    }
}
