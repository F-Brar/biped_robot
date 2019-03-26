using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSV
{
    
    private String fileLocation = Application.dataPath + "\\Curriculum_CSV\\";//"C:\\Users\\Telefon Mann\\Desktop\\MinfTogetherStudie\\outputData\\";
    private string folderName = "\\Data";
    private int folderCounter;

    private string curriculumName;
    private int lesson;
    private int completionSteps;
    

    private StreamWriter curriculumDataWriter;

    public CSV(string curriculumName, int lesson, int completionSteps, bool firstLog)
    {
        this.curriculumName = curriculumName;
        this.lesson = lesson;
        this.completionSteps = completionSteps;
        
        if (!Directory.Exists(fileLocation))
        {
            Directory.CreateDirectory(fileLocation);
        }
        /*
         * //string.Format("{0}/{1:D04} shot.png", folder, Time.frameCount);
        //create a new folder for new curriculum
        folderName += folderCounter;
        var result = fileLocation.Substring(fileLocation.Length - 3);
        Int32.TryParse(result, out folderCounter);

        folderCounter++;

        var replacement = folderName.Replace(result.ToString(), folderCounter.ToString());

        fileLocation += folderName + "\\";

        if (firstLog)
        {
            Directory.CreateDirectory(fileLocation);
        }*/
        createWriters();
    }

    private void createWriters()
    {
        //looks for existing files and increases the version counter
        int dataWriterCounter = 0;


        
        
        while (true)
        {

            
            if (File.Exists(fileLocation + "curriculumData" + dataWriterCounter + ".csv"))
            {
                
                dataWriterCounter++;
            }
            else
            {
                break;
            }
        }


        //creating save writers

        curriculumDataWriter = new System.IO.StreamWriter(fileLocation + "curriculumData" + dataWriterCounter + ".csv", true);
        //curriculumDataWriter.WriteLine("take start: " + getCurrentTimeMillis());
        curriculumDataWriter.WriteLine("Name;Lesson;CompletionSteps;");
        curriculumDataWriter.Flush();

    }

    public void SaveCSV()
    {
        String result = curriculumName + ";" + lesson.ToString() + ";" + completionSteps;
        writeData(result);
    }

    public void writeData(String result)
    {
        curriculumDataWriter.WriteLine(result);
        curriculumDataWriter.Flush();
    }
}
