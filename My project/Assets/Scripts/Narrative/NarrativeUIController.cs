using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace GalacticNexus.Scripts.Narrative
{
    public class NarrativeUIController : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject WindowRoot;
        public TextMeshProUGUI DialogueText;
        public TextMeshProUGUI CharacterNameText;
        public Image CharacterPortrait;
        
        [Header("Portraits")]
        public Sprite EnforcerPortrait;
        public Sprite VoidWalkerPortrait;
        public Sprite CoreOfficerPortrait;

        [Header("Settings")]
        public float TypeSpeed = 0.05f;
        
        private Coroutine typingCoroutine;
        private bool isWaitingForClick;

        private void Awake()
        {
            if (WindowRoot) WindowRoot.SetActive(false);
        }

        public void ShowMessage(string CharacterName, string message, float eventCode)
        {
            if (WindowRoot == null) return;
            
            WindowRoot.SetActive(true);
            CharacterNameText.text = CharacterName;
            
            // Set Portrait based on name or code
            if (CharacterName.Contains("Sindicato")) CharacterPortrait.sprite = EnforcerPortrait;
            else if (CharacterName.Contains("Void")) CharacterPortrait.sprite = VoidWalkerPortrait;
            else CharacterPortrait.sprite = CoreOfficerPortrait;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(message));
        }

        private IEnumerator TypeText(string message)
        {
            DialogueText.text = "";
            foreach (char c in message.ToCharArray())
            {
                DialogueText.text += c;
                yield return new WaitForSeconds(TypeSpeed);
            }
            
            isWaitingForClick = true;
        }

        private void Update()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (isWaitingForClick && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                isWaitingForClick = false;
                WindowRoot.SetActive(false);
            }
        }
    }
}
