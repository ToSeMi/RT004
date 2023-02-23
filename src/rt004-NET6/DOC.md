# Checkpoint 01 documentation

## Reading input

The program can either read a specified json file as config, reading the parameters from the command line. If there are no arguments, the program will check if there is not a file named "conf.json" and reads it if exists. If not, the default parameters are set.

\[program name\]  
\[program name\] \[name of json config file\]  
\[program name\] \[width\] \[height\] \[output filename\]  

## Output

Program's output is a .pfm image, it should be a filled-in circle.