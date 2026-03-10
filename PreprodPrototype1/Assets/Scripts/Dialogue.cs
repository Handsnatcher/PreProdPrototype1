using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed; //character per second
    private AudioSource source;
    public AudioClip soundEffectClip;

    private int index;

    // Start is called before the first frame update
    void Start()
    {
        textComponent.text = string.Empty;
        BeginDialogue();
        source = GetComponent<AudioSource>();
    }

    //check if player skipped through dialogue
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (textComponent.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                //instantly fill out line
                textComponent.text = lines[index];
            }
        }
    }

    void BeginDialogue()
    {
        index = 0;
        StartCoroutine(ShowLine());
        PlayDialogueSound();
    }

    IEnumerator ShowLine()
    {
        //show characters
        foreach (char character in lines[index].ToCharArray())
        {
            textComponent.text += character;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    //TODO: change pitch randomnly, play for length of text?
    void PlayDialogueSound()
    {
        if (source != null && soundEffectClip != null)
        {
            source.PlayOneShot(soundEffectClip, 0.5f);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(ShowLine());
            PlayDialogueSound();
        }
        else
        {
            gameObject.SetActive(false);
            ReturnToLevelMenu();
        }
    }

    void ReturnToLevelMenu()
    {
        SceneManager.LoadScene("MapScene");
    }
}
