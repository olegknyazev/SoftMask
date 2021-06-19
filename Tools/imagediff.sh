#!/bin/sh
if output=$(perceptualdiff $2 $5); then
  echo $1: same
else
  echo $1: differs
  echo $output
fi