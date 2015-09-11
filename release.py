#!/usr/bin/python3

# Copyright (C) 2015 haha01haha01

# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.

import os
import shutil
import sys

PROD_DIR = "Production"
RELEASE_DIRS = [r"HaMusic\bin\Release", r"HaMusicServer\bin\Release", r"HaMusicLib\bin\Release"]

DEBUG = len(sys.argv) > 1 and sys.argv[1] == "-g"

def whitelist(f):
    return (f.endswith(".exe.config") and not f.endswith(".vshost.exe.config")) or \
           (f.endswith(".exe") and not f.endswith(".vshost.exe")) or \
           f.endswith(".dll") or \
           (DEBUG and f.endswith(".pdb"))

def main():
    if os.path.exists(PROD_DIR):
        shutil.rmtree(PROD_DIR)
    os.mkdir(PROD_DIR)
    for RELEASE_DIR in RELEASE_DIRS:
        for (rls_path, dirs, files) in os.walk(RELEASE_DIR):
            prod_path = PROD_DIR + rls_path[len(RELEASE_DIR):]
            for d in dirs:
                os.mkdir(os.path.join(prod_path, d))
            for f in files:
                if whitelist(f):
                    shutil.copyfile(os.path.join(rls_path, f), os.path.join(prod_path, f))

if __name__ == "__main__":
    main()
