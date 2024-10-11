using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameHandler : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject gamePlayUI;

    [SerializeField] private TMPro.TextMeshProUGUI scoreText;
    [SerializeField] private TMPro.TextMeshProUGUI finalScoreText;

    private LandingPointManager landingManager;
    private ShipController shipController;
    private float score = 0f;

    private float savedFuel;

    private void Awake()
    {

        if (SceneManager.GetActiveScene().name == "MenueScene")
        {

            return;
        }

        shipController = FindObjectOfType<ShipController>();

        landingManager = GetComponent<LandingPointManager>();
        gameOverUI.SetActive(false);
        gamePlayUI.SetActive(true);

        savedFuel = shipController.GetFuel();
        scoreText.text = "Score\n" + "0";
    }

    public void GameOver()
    {
        Cursor.lockState = CursorLockMode.None;
        gameOverUI.SetActive(true);
        gamePlayUI.SetActive(false);

        gameOverUI.GetComponent<Animator>().Play("GameOver");

        // show the final score
        if(score == 0)
        {
            finalScoreText.text = "Final Score\n" + "0";
            return;
        }
        finalScoreText.text = "Final Score\n" + score.ToString("#.");

    }

    public void Landed()
    {
        // get fuel used since last landing
        float fuelUsed = savedFuel - shipController.GetFuel();
        // calculate the percentage of fuel used since last landing
        float fuelUsedPercentage = (fuelUsed / savedFuel) * 100;

        // exponentially increase the score based on the percentage of fuel saved
        score += Mathf.Pow(100 - fuelUsedPercentage,1.8929f);


        scoreText.text = "Score\n" + score.ToString("#.");
        savedFuel = shipController.GetFuel();

        landingManager.GenerateLandingPoint();
    }

    public void Menu()
    {
        SceneManager.LoadScene(0);
    }

    public void Restart()
    {
        SceneManager.LoadScene(1);
    }

    public void Play()
    {
        SceneManager.LoadScene(1);
        shipController.inMenue = false;
    }

    public void Exit()
    {
        Application.Quit();
    }
}
