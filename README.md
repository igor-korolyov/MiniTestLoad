# MiniTestLoad
Minimalistic HTTP load-testing tool written in C#

## Usage notes

### Mode of operation

There are 2 main modes of operation implemented in this tool:
- Defined number of repetitions (default).
- Defined duration of work.
The second mode is activated with the command-line switch `-d number_of_seconds` (without this switch, we'll run in the first mode with preconfigured defaults).

### Command-line switches

There are 2 ways to specify command-line switches, that are completely identical and interchangeable:
- Single-letter codes (prepended with a single dash).
- Full option names (prepended with double dashes).
A short usage manual is displayed if you do not specify any parameters (there is a required one), if you have errors in parameters, or if you pass the `-- help` switch.

Individual options are:
- `-t` or `--threads` - specifies the number of parallel threads sending requests (default value is 1). Followed by a number. The allowable range is from 1 to 20.
- `-r` or `--threshold` - specifies the time in milliseconds used to detect slow responses (default value is 1000, which is 1 second). Slow responses are counted separately and are printed in the log section of the application's UI. Followed by a number. Should be greater than 0.
- `-n` or `--count` - specifies the number of times request batches are sent. Has meaning only in the "Defined number of repetitions" mode. Each spawned thread will send requests based on this value, so the total number of requests will be `count * threads * number_of_requests_in_a_batch`. Followed by a number. The allowable range is from 1 to 10 000.
- `-d` or `--duration` - specifies the duration of the test run in seconds. It turns on the "Defined duration of work" mode if specified. After the specified time passes, all active requests will be cancelled. This option is mutually exclusive with the previous one - it will be an error to specify both. Followed by a number. The allowable range is from 1 to 86 400 (which is 1 day).
- `-a` or `--auth` - specifies a file containing authorization header data (usually it is a `Bearer ey...` line). This data is appended to each request sent by the tool. This is a handy way to hold authorization data (that is changed pretty often due to token expiration) in a single place and apply it globally. **Be aware that any tokens (in auth files or in request files) are subject to security precautions - do not publish them, share them with strangers, or on the Internet (unless you are sure that no one can use them for malicious purposes).** Followed by a file name (either a file located in the current working folder, or a file with a full path).
- `--help` - displays short usage notes.
- `--version` - displays version information.

All options are optional 😄

A mandatory part of a command-line syntax is a list of request files (it should contain at least one item). Files can be specified interchangeably with other options, though it is handy to keep them together either as the first thing just after the tool name or after other options. You can use just the file name, if it is located in the current folder, or a full path to the file.
The tool will read all files once during startup, so any changes made afterward will not be seen.
All the specified files in the exact order make up a batch. They will be repeated in the same order a specified number of times, or until a specified period of time expires.

### Auth file structure

The authorization data file should contain a single line, containing a schema and data separated by a space character. Its contents will be passed directly as the value of the `Authorization:` header.

### Request file structure

The request file consists of 3 parts, of which only the first is mandatory:
- The method and URL line.
- Headers.
- Body.

Comments are lines starting with either the `#` symbol or `//` symbols. They are allowed only in the "Method and URL" and "Headers" sections. Blank lines are allowed only before the real "Method and URL" line - after that, the blank line will signal the start of the "Body" section.

The method and URL line should be in the following format: `METHOD http[s]://host[:port]/path/to/method[?query=params&if=needed]`. Any method is supported (GET/POST/PUT/DELETE/etc.), query parameters are supported as well.

Headers are optional, if needed, they should be in the common http format, e.g. `X-Key: API-Key-Value`. Each header should be on its own line. Blank lines are not allowed (as they designate where the body section starts). Multiple headers (headers with the same name) are allowed, though they will be folded into a single header with values separated by commas (according to the HTTP specification).

The body section will be transferred as is, as a plain text, though it will be marked as a `Content-Type: application/json; charset=utf-8` by default. You can specify the `Content-Type:` header explicitly if you need another value.

### Display

The tool will manage the console to display its UI - it will occupy all visual space.
There are four different parts of the UI:
- Title line. There we can see a spinner (rotating while the tool is working), the number of worker threads, the number of requests in a batch, an operational mode (with its parameters - the number of iterations or a time duration), a threshold for slow response detection, and a global authorization file in use (if any).
- Statistics area for worker threads. There we can see a line for each worker thread with its data: thread number, the number of sent requests, the number of received responses, the number of failures (any non-2xx response is considered a failure), the number of "long" responses (above the configured threshold), and timing - minimum, maximum, and average response time in milliseconds.
- Log area. "Long" requests will be logged here, together with some other messages.
- Status line. Just a reminder about Ctrl+C, which can be used to stop an application.

![image](https://github.com/igor-korolyov/MiniTestLoad/assets/56341209/341f39eb-7fc5-4103-8cb9-3264145ea607)
