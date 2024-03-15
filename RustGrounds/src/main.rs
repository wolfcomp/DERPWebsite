use uwuifier::uwuify_str_sse;
use std::io;

fn main() {
    while true {
        println!("Enter a sentence to uwuify: ");
        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();
        let uwuified = uwuify_str_sse(&input);
        println!("{}", uwuified);
    }
}
