#!/bin/bash
tmux new-session -d -s discordbot 'dotnet run'
tmux attach -t discordbot